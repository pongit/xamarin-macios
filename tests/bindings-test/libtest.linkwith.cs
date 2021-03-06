using System;
#if __UNIFIED__
using ObjCRuntime;
#else
using MonoTouch.ObjCRuntime;
#endif
using System.Runtime.InteropServices;

[assembly: LinkWith ("libtest.a", LinkTarget.Simulator | LinkTarget.ArmV6 | LinkTarget.ArmV7 | LinkTarget.ArmV7s | LinkTarget.Arm64 | LinkTarget.Simulator64, SmartLink = true, Frameworks = "Foundation" , LinkerFlags = "-lz")]

public static class LibTest {
	[DllImport ("__Internal")]
	public static extern int theUltimateAnswer ();
}
