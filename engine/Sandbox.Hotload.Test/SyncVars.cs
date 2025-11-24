extern alias After;
extern alias Before;

using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sandbox;

// ReSharper disable PossibleNullReferenceException

namespace Hotload
{
	[TestClass]
	public class SyncVarTests : HotloadTests
	{
		[TestMethod]
		public void TestAddedSyncVar()
		{
			var properties = ReflectionQueryCache.SyncProperties( typeof( Before::TestClass47 ) );
			Assert.AreEqual( 0, properties.Count() );

			Hotload();

			// Clear the type cache so next time we'll pick up the added [Sync] attribute
			ReflectionQueryCache.ClearTypeCache();

			properties = ReflectionQueryCache.SyncProperties( typeof( After::TestClass47 ) );

			Assert.AreEqual( 1, properties.Count() );
		}
	}
}
