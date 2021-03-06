﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using myWorkSafe;
using myWorkSafe.Groups;
using NUnit.Framework;
using SIL.IO;


namespace WorkSafe.Tests
{
	[TestFixture]
	public class ScannerTests
	{
		private DirectoryScanner _scanner;
		[SetUp]
		public void Setup()
		{
			_scanner = new DirectoryScanner();
		}

		[Test]
		public void Scan_OnSourceCode_FindsLotsOfFiles()
		{
			var files = _scanner.Scan(FileLocator.DirectoryOfApplicationOrSolution);
			Assert.That(files.Count(), Is.GreaterThan(200));
		}
	}
}
