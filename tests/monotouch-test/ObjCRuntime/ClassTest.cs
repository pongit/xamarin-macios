//
// Unit tests for Class
//
// Authors:
//	Sebastien Pouliot <sebastien@xamarin.com>
//
// Copyright 2012 Xamarin Inc. All rights reserved.
//

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

#if XAMCORE_2_0
using Foundation;
using ObjCRuntime;
#else
using MonoTouch.Foundation;
using MonoTouch.ObjCRuntime;
#endif
using NUnit.Framework;

namespace MonoTouchFixtures.ObjCRuntime {
	
	[TestFixture]
	[Preserve (AllMembers = true)]
	public class ClassTest {
		[DllImport ("/usr/lib/libobjc.dylib")]
		extern static IntPtr objc_getClass (string name);
		
		// based on https://xamarin.assistly.com/agent/case/6816
		[Register ("ZählerObject")]
		class ZählerObject : NSObject {
		}
		
		[Test]
		public void getClassTest ()
		{
			IntPtr p = objc_getClass ("ZählerNotExists");
			Assert.That (p, Is.EqualTo (IntPtr.Zero), "DoesNotExists");

			p = objc_getClass ("ZählerObject");
			Assert.That (p, Is.Not.EqualTo (IntPtr.Zero), "ä");
		}
		
		[Test]
		public void LookupTest ()
		{
			IntPtr p = objc_getClass ("ZählerObject");
			var m = typeof (Class).GetMethod ("Lookup", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof (IntPtr) }, null);
			Type t = (Type) m.Invoke (null, new object [] { objc_getClass ("ZählerObject") });
			Assert.That (t, Is.EqualTo (typeof (ZählerObject)), "Lookup");
			Assert.That (p, Is.Not.EqualTo (IntPtr.Zero), "Class");
		}

		// Not sure what to do about this one, it doesn't compile with the static registrar (since linking fails)
#if DYNAMIC_REGISTRAR
		[Test]
		public void ThrowOnMissingNativeClassTest ()
		{
			bool saved = Class.ThrowOnInitFailure;

			Class.ThrowOnInitFailure = true;
			try {
				Assert.Throws<Exception> (() => new InexistentClass (), "a");
			} finally {
				Class.ThrowOnInitFailure = saved;
			}
		}

		[Register ("Inexistent", true)]
		public class InexistentClass : NSObject {
			public override IntPtr ClassHandle {
				get {
					return Class.GetHandle (GetType ().Name);
				}
			}
		}
#endif

		[Test]
		public void Bug33981 ()
		{
			var types = new List<Type> ();
			foreach (var type in GetType ().Assembly.GetTypes ()) {
				if (type.IsSubclassOf (typeof (NSObject)) && type.Name.StartsWith ("BUG33981"))
					types.Add (type);
			}

			Assert.That (types.Count, Is.GreaterThan (50), "test type enumeration");

			const int n = 5;
			var threads = new Thread [n];
			var cntr = new int [n];
			Exception ex = null;
			var startPistol = new ManualResetEvent (false);
			var stopLine = new CountdownEvent (n);
			for (int i = 0; i < n; i++) {
				var idx = i;
				threads [i] = new Thread (() => {
					startPistol.WaitOne ();
					try {
						foreach (var type in types) {
							var c = Class.GetHandle (type.Name);
							if (c != IntPtr.Zero) {
								try {
									Class.Lookup (new Class (c));
								} catch (Exception e) {
									ex = e;
									return;
								}
								cntr [idx]++;
							}
						}
					} finally {
						stopLine.Signal ();
					}
				});
				threads [i].IsBackground = true;
				threads [i].Start ();
			}
			startPistol.Set ();
			stopLine.Wait ();

			Assert.IsNull (ex);
		}
	}
}
