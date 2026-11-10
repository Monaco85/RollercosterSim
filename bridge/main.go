// Achterbahn-Bruecke: zeigt die Seite an und reicht die Bewegungsdaten als UDP an SimHub weiter.
package main

import (
	"bytes"
	_ "embed"
	"encoding/binary"
	"encoding/json"
	"fmt"
	"math"
	"math/rand"
	"net"
	"net/http"
	"os"
	"os/exec"
	"path/filepath"
	"runtime"
	"strconv"
	"strings"
	"sync"
	"time"

	"github.com/gorilla/websocket"
)

//go:embed index.html
var page []byte

//go:embed setup.ps1
var setupScript []byte

const addr = "127.0.0.1:8765"
const frameLen = 31

type link struct {
	sync.Mutex
	last   [frameLen]float32
	seen   time.Time
	simhub bool
}

var st link
var telemetrySig uint32

// Paket wie von SimHubs Generator vorgegeben: Pack=1, little endian.
func buildPacket(f *[frameLen]float32, emitter, session, counter uint64, disc uint32, millis uint32, running, paused bool) []byte {
	b := &bytes.Buffer{}
	w := func(v any) { binary.Write(b, binary.LittleEndian, v) }
	bit := func(x bool) uint8 {
		if x {
			return 1
		}
		return 0
	}
	w(gameSignature())
	w(telemetrySig)
	w(uint16(1))
	w(uint16(0))
	w(emitter)
	w(uint8(0))
	w(counter)
	w(bit(running))
	w(bit(paused))
	w(session)
	w(uint8(0)) // Replay
	w(bit(running))
	w(uint8(0)) // AI
	w(uint8(0)) // Zuschauer
	w(float64(f[3]))
	w(disc)
	// Standardfelder
	for _, i := range []int{4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15} {
		w(f[i])
	}
	w(float64(f[16]))
	w(float64(f[17]))
	w(float64(f[18]))
	w(f[19]) // SpeedKmh
	w(f[19]) // GroundSpeedKmh
	w(f[20])
	w(f[21])
	w(f[22])
	// eigene Felder
	w(millis)
	w(float32(0.01))
	w(f[13])
	w(f[19] * 0.621371)
	w(float32(math.Min(1, float64(f[20])/7000)))
	w(float32(0))
	w(f[23])
	w(f[24])
	w(f[25])
	w(bit(f[27] > 0))
	w(bit(f[28] > 0))
	w(bit(f[29] > 0))
	w(bit(f[26] > 0))
	w(bit(f[30] > 0))
	return b.Bytes()
}

func sender() {
	dst := &net.UDPAddr{IP: net.IPv4(127, 0, 0, 1), Port: defaultPort}
	c, err := net.DialUDP("udp4", nil, dst)
	if err != nil {
		fmt.Println("UDP-Fehler:", err)
		return
	}
	emitter, session := rand.Uint64()|1, rand.Uint64()|1
	var counter uint64
	var cur [frameLen]float32
	prevDisc := float32(-1)
	var disc uint32
	t0 := time.Now()
	tick := time.NewTicker(10 * time.Millisecond)
	nextProbe := time.Time{}
	for now := range tick.C {
		st.Lock()
		f := st.last
		seen := st.seen
		st.Unlock()
		fresh := now.Sub(seen) < 300*time.Millisecond
		if fresh {
			cur = f
			if f[2] != prevDisc {
				prevDisc = f[2]
				disc++
				session = rand.Uint64() | 1
			}
		} else {
			// Seite weg oder eingefroren: Bewegung weich auf null, egal was vorher lag
			for _, i := range []int{5, 6, 7, 8, 9, 10, 11, 12} {
				cur[i] *= 0.985
			}
			cur[1] = 1
		}
		counter++
		cur3 := cur
		cur3[3] = float32(now.Sub(t0).Seconds())
		if telemetrySig != 0 {
			pkt := buildPacket(&cur3, emitter, session, counter, disc, uint32(now.Sub(t0).Milliseconds()), cur[0] > 0, cur[1] > 0)
			c.Write(pkt)
		}
		if now.After(nextProbe) {
			nextProbe = now.Add(2 * time.Second)
			st.Lock()
			st.simhub = portTaken()
			st.Unlock()
		}
	}
}

// SimHub hoert erst mit, wenn das Spiel dort aktiviert ist. Laesst sich der Port nicht belegen, ist es so weit.
func portTaken() bool {
	l, err := net.ListenUDP("udp4", &net.UDPAddr{Port: defaultPort})
	if err != nil {
		return true
	}
	l.Close()
	return false
}

var up = websocket.Upgrader{CheckOrigin: func(r *http.Request) bool {
	h := r.Header.Get("Origin")
	return h == "" || h == "http://"+addr || h == "http://localhost:8765"
}}

func ws(w http.ResponseWriter, r *http.Request) {
	c, err := up.Upgrade(w, r, nil)
	if err != nil {
		return
	}
	defer c.Close()
	done := make(chan struct{})
	defer close(done)
	go func() {
		t := time.NewTicker(time.Second)
		defer t.Stop()
		for {
			select {
			case <-done:
				return
			case <-t.C:
				st.Lock()
				m, _ := json.Marshal(map[string]bool{"simhub": st.simhub, "sig": telemetrySig != 0})
				st.Unlock()
				if c.WriteMessage(websocket.TextMessage, m) != nil {
					return
				}
			}
		}
	}()
	for {
		_, m, err := c.ReadMessage()
		if err != nil {
			return
		}
		if len(m) != frameLen*4 {
			continue
		}
		var f [frameLen]float32
		for i := range f {
			f[i] = math.Float32frombits(binary.LittleEndian.Uint32(m[i*4:]))
		}
		for _, x := range f {
			if x != x || math.IsInf(float64(x), 0) {
				return
			}
		}
		st.Lock()
		st.last, st.seen = f, time.Now()
		st.Unlock()
	}
}

// Einrichten: Definition schreiben, Signatur von SimHub ausrechnen lassen.
func install() {
	if s := strings.TrimSpace(os.Getenv("ACHTERBAHN_SIG")); s != "" {
		if v, err := strconv.ParseUint(s, 0, 32); err == nil {
			telemetrySig = uint32(v)
			return
		}
	}
	if runtime.GOOS != "windows" {
		telemetrySig = 0x12345678
		return
	}
	def := simdefPath()
	os.MkdirAll(filepath.Dir(def), 0o755)
	os.MkdirAll(filepath.Dir(linkPath()), 0o755)
	old := readSignature()
	data, _ := simdefJSON(old)
	os.WriteFile(def, data, 0o644)
	os.WriteFile(linkPath(), []byte(def), 0o644)

	script := filepath.Join(filepath.Dir(def), "setup.ps1")
	os.WriteFile(script, setupScript, 0o644)
	sys := os.Getenv("SystemRoot")
	var lastErr string
	for _, ps := range []string{sys + `\System32\WindowsPowerShell\v1.0\powershell.exe`, sys + `\SysWOW64\WindowsPowerShell\v1.0\powershell.exe`} {
		out, err := exec.Command(ps, "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script, "-Definition", def).CombinedOutput()
		for _, l := range strings.Split(string(out), "\n") {
			p := strings.Fields(l)
			if len(p) == 4 && p[0] == "SIG" {
				g, _ := strconv.ParseUint(p[1], 10, 32)
				t, _ := strconv.ParseUint(p[2], 10, 32)
				n, _ := strconv.Atoi(p[3])
				if uint32(g) != gameSignature() {
					fmt.Printf("WARNUNG: SimHub rechnet eine andere Spielkennung (%08X).\n", g)
				}
				if n != expectedPacket {
					fmt.Printf("FEHLER: SimHub erwartet %d Bytes, gesendet werden %d. SimHub-Format hat sich geaendert.\n", n, expectedPacket)
					return
				}
				telemetrySig = uint32(t)
				fmt.Printf("SimHub-Einrichtung OK (Signatur %08X, %d Bytes).\n", t, n)
				if old == 0 {
					fmt.Println("\n>>> Erststart: SimHub einmal neu starten und das Spiel \"Achterbahn\" in der Spieleliste aktivieren.")
				}
				return
			}
		}
		if err != nil {
			lastErr = strings.TrimSpace(string(out))
		}
	}
	if old != 0 {
		telemetrySig = old
		fmt.Println("Signatur aus der vorhandenen Definition uebernommen.")
		return
	}
	fmt.Println("SimHub-Einrichtung fehlgeschlagen:", lastErr)
	fmt.Println("Bitte SimHub auf den neuesten Stand bringen (mindestens 9.11.5). Die Fahrt laeuft trotzdem, nur ohne Sitz.")
}

func open() {
	url := "http://" + addr + "/"
	if runtime.GOOS != "windows" {
		return
	}
	for _, p := range []string{os.Getenv("ProgramFiles(x86)") + `\Microsoft\Edge\Application\msedge.exe`, os.Getenv("ProgramFiles") + `\Microsoft\Edge\Application\msedge.exe`} {
		if _, err := os.Stat(p); err == nil {
			if exec.Command(p, "--app="+url, "--start-fullscreen", "--autoplay-policy=no-user-gesture-required").Start() == nil {
				return
			}
		}
	}
	exec.Command("rundll32", "url.dll,FileProtocolHandler", url).Start()
}

func main() {
	fmt.Println("Achterbahn fuer den Rennsitz")
	fmt.Println("Fenster offen lassen, solange gefahren wird.")
	install()
	go sender()
	mux := http.NewServeMux()
	html := append([]byte("<!doctype html><html lang=\"de\"><head><meta charset=\"utf-8\">"), page...)
	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "text/html; charset=utf-8")
		w.Write(html)
	})
	mux.HandleFunc("/telemetrie", ws)
	l, err := net.Listen("tcp4", addr)
	if err != nil {
		fmt.Println("Port 8765 ist belegt. Laeuft die Achterbahn schon? Sonst Fenster schliessen und neu starten.")
		time.Sleep(6 * time.Second)
		return
	}
	go open()
	fmt.Println("Laeuft auf http://" + addr)
	http.Serve(l, mux)
}
