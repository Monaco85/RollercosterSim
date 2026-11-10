package main

import (
	"encoding/json"
	"hash/crc32"
	"os"
	"path/filepath"
	"strings"
)

// Die Kennung der Simulation. Darf sich nie aendern, sonst vergisst SimHub die Einstellungen.
const uniqueID = "6f1d2c7a-3b52-4e0e-9a41-7c5de2b8a901"
const simName = "Achterbahn"
const defaultPort = 20790

// GameSignature ist bei SimHub das CRC32 der kleingeschriebenen UniqueId
// (an den Definitionen zweier Fremdprojekte nachgerechnet).
func gameSignature() uint32 { return crc32.ChecksumIEEE([]byte(strings.ToLower(uniqueID))) }

type field struct {
	FieldType  string `json:"FieldType,omitempty"`
	Name       string `json:"Name,omitempty"`
	Comment    string `json:"Comment,omitempty"`
	BinaryType int    `json:"BinaryType,omitempty"`
	TypeName   string `json:"TypeName"`
}

func std(n string) field { return field{FieldType: n, TypeName: "StandardField"} }
func custom(n, c string, t int) field {
	return field{Name: n, Comment: c, BinaryType: t, TypeName: "CustomTelemetryField"}
}

const (
	u8  = 2
	u32 = 6
	f32 = 8
)

// Reihenfolge = Reihenfolge im Paket. Aenderung nur zusammen mit buildPacket.
func fields() []field {
	var l []field
	for _, n := range []string{"YawDegrees", "PitchDegrees", "RollDegrees", "YawRateDegreesPerSecond", "PitchRateDegreesPerSecond", "RollRateDegreesPerSecond",
		"LocalSurgeMs2", "LocalSwayMs2", "LocalHeaveMs2", "LocalVelocityForwardMps", "LocalVelocityLateralMps", "LocalVelocityUpMps",
		"VehiclePositionNorth", "VehiclePositionEast", "VehiclePositionUp", "SpeedKmh", "GroundSpeedKmh", "EngineRpm", "Throttle", "Brake"} {
		l = append(l, std(n))
	}
	return append(l,
		custom("TimestampMS", "Zeit in Millisekunden", u32),
		custom("DeltaTimeSeconds", "Zeitschritt in Sekunden", f32),
		custom("SpeedMps", "Tempo in m/s", f32),
		custom("SpeedMph", "Tempo in mph", f32),
		custom("EngineRpmRatio", "Rumpeln passend zum Tempo, 0 bis 1", f32),
		custom("Steering", "Immer 0", f32),
		custom("GForceLateral", "Seitlicher Druck in G", f32),
		custom("GForceVertical", "Sitzdruck in G", f32),
		custom("GForceLongitudinal", "Druck nach hinten in G", f32),
		custom("OnLift", "Kettenlift aktiv", u8),
		custom("OnLaunch", "Abschuss aktiv", u8),
		custom("OnBrake", "Bremse aktiv", u8),
		custom("Airtime", "Schwerelos", u8),
		custom("LandingEvent", "Ein Paket lang nach Airtime", u8),
	)
}

const expectedPacket = 188

func baseDir() string {
	d, err := os.UserCacheDir() // unter Windows: %LOCALAPPDATA%
	if err != nil {
		d = os.TempDir()
	}
	return filepath.Join(d, "SimHub", "ExternalSims")
}

func simdefPath() string { return filepath.Join(baseDir(), "Definitions", simName, simName+".simdef") }
func linkPath() string   { return filepath.Join(baseDir(), "Registrations", uniqueID+".shlink") }

func simdefJSON(telemetrySig uint32) ([]byte, error) {
	d := map[string]any{
		"TelemetryBehaviour": map[string]any{"GameFamily": 1, "DefaultMaxSpeed": 120.0, "HasRumbleStripsContactSurface": false, "AllowGimbleLockSanitizer": false},
		"TelemetryDefinition": map[string]any{"LayoutMajorVersion": 1, "LayoutMinorVersion": 0, "ProtocolVersion": 1, "Fields": fields()},
		"GameSignature":       gameSignature(),
		"TelemetrySignature":  telemetrySig,
		"Name":                simName,
		"UniqueId":            uniqueID,
		"DefaultUDPPort":      defaultPort,
		"DetectionProcesses":  []any{map[string]any{"ProcessName": "Achterbahn", "FileChecks": []any{}}},
		"SimStartCommand":     nil, "SimStartArguments": nil, "UseExtractorProcess": false, "ExtractorProcessCommand": nil, "IconPath": nil,
	}
	return json.MarshalIndent(d, "", "  ")
}

func readSignature() uint32 {
	b, err := os.ReadFile(simdefPath())
	if err != nil {
		return 0
	}
	var d struct {
		TelemetrySignature uint32
	}
	if json.Unmarshal(b, &d) != nil {
		return 0
	}
	return d.TelemetrySignature
}
