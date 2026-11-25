using UnityEngine.InputSystem; using UnityEngine.InputSystem.Controls;
// Listet Joysticks/Lenkraeder mit Achsen und aktuellen Werten (nur lesen).
public static class AgentDevices { public static string Main() {
  var sb = new System.Text.StringBuilder();
  foreach (var d in InputSystem.devices)
  {
    sb.Append(d.GetType().Name + " '" + d.displayName + "' product='" + d.description.product + "' layout=" + d.layout + "\n");
    if (!(d is Joystick) && !(d is Gamepad)) continue;
    foreach (var c in d.allControls)
      if (c is AxisControl a && !(c is ButtonControl) && !c.synthetic) sb.Append("   " + c.path + " = " + a.ReadValue().ToString("0.000") + (c.noisy ? " (noisy)" : "") + "\n");
  }
  return sb.ToString(); } }
