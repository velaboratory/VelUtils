using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VelUtils;

public class VelUtilsTests
{
	// A Test behaves as an ordinary method
	[Test]
	public void SetBoolGetBool()
	{
		PlayerPrefsJson.SetBool("testBool", true);
		Assert.AreEqual(true, PlayerPrefsJson.GetBool("testBool"));
	}

	[Test]
	public void SetStringGetBool()
	{
		PlayerPrefsJson.SetString("testWrongType", "true");
		Assert.AreEqual(false, PlayerPrefsJson.GetBool("testWrongType", false));
	}
	
	[Test]
	public void SetIntGetString()
	{
		PlayerPrefsJson.SetInt("testWrongType2", 2);
		Assert.AreEqual("2", PlayerPrefsJson.GetString("testWrongType2", "3"));
	}
	
	[Test]
	public void SetStringGetInt()
	{
		PlayerPrefsJson.SetString("testWrongType3", "2");
		Assert.AreEqual(3, PlayerPrefsJson.GetInt("testWrongType3", 3));
	}

	// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
	// `yield return null;` to skip a frame.
	[UnityTest]
	public IEnumerator VelUtilsTestsWithEnumeratorPasses()
	{
		// Use the Assert class to test conditions.
		// Use yield to skip a frame.
		yield return null;
	}
}