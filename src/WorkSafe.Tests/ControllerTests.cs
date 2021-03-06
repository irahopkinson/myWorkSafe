﻿using System;
using System.Collections.Generic;using myWorkSafe;
using myWorkSafe.Groups;
using System.IO;
using Microsoft.Experimental.IO;
using NUnit.Framework;
using SIL.TestUtilities;
using SIL.Extensions;
using SIL.Progress;

namespace WorkSafe.Tests
{
	[TestFixture]
	public class ControllerTests
	{
		[TestAttribute]
		[Ignore("Preview Not Implemented in new engine")]
		public void Preview_EmptyDest_SingleFileWillBeCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"),"Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "Blah blah blah");
				var source = new RawDirectoryGroup("1",from.Path,null,null);
				var groups = new List<FileGroup>(new[] {source});        
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());

				sync.GatherPreview();
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(2, source.NewFileCount);
				Assert.AreEqual(23, source.NetChangeInBytes);
			}
		}

		[TestAttribute]
		[Ignore("Preview Not Implemented in new engine")]
		public void Preview_FileExistsButHasChanged_WillBeReplaced()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "dee dee dee");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();
				sync.Run();
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah Blah Blah Blah");
				sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(15, source.NetChangeInBytes);
			}
		}

		/// <summary>
		/// NB: the result of this logic is that a file which is first selected by a "wesay" group,
		/// and if found in documents/wesay, won't
		/// appear in the backup under "documents" folder produced by a subsequent "all documents" group.
		/// If the order of the two groups is reversed, then the whole "wesay" group could be empty, as
		/// all the files were already accounted for.
		/// </summary>
		[TestAttribute]
		[Ignore("Preview Not Implemented in new engine")]
		public void Preview_FileIncludedInPreviousGroup_WontBeCountedTwice()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source1 = new RawDirectoryGroup("1",from.Path,null,null);
				var source2 = new RawDirectoryGroup("2",from.Path,null,null);
				var groups = new List<FileGroup>(new[] { source1, source2 });
				var progress = new StringBuilderProgress(){ShowVerbose=true};
				var sync = new MirrorController(to.Path, groups, 100, progress);
				sync.GatherPreview();

				Assert.AreEqual(1, source1.NewFileCount);
				Assert.AreEqual(0, source1.UpdateFileCount);
				Assert.AreEqual(0, source1.DeleteFileCount);
				Assert.AreEqual(9, source1.NetChangeInBytes);

				Assert.AreEqual(0, source2.NewFileCount);
				Assert.AreEqual(0, source2.UpdateFileCount);
				Assert.AreEqual(0, source2.DeleteFileCount);
				Assert.AreEqual(0, source2.NetChangeInBytes);
			}
		}

		[TestAttribute]
		[Ignore("Preview Not Implemented in new engine")]
		public void Preview_FolderExcluded_WillBeSkipped()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				Directory.CreateDirectory(from.Combine("sub"));
				File.WriteAllText(from.Combine("sub", "one.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				source.Filter.SubdirectoryExcludes.Add("sub");

				var groups = new List<FileGroup>(new[] { source});
				var progress = new StringBuilderProgress() { ShowVerbose = true };
				var sync = new MirrorController(to.Path, groups, 100, progress);
				sync.GatherPreview();

			// we don't get this progress yet	Assert.That(progress.Text.ToLower().Contains("skip"));
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
			}
		}
		[TestAttribute]
		[Ignore("Preview Not Implemented in new engine")]
		public void Preview_FilteredFile_WontBeCounted()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("text.txt"), "Blah blah");
				File.WriteAllText(from.Combine("info.info"), "deedeedee");
				var source1 = new RawDirectoryGroup("1", from.Path, new []{"*.info"}, null);
				var groups = new List<FileGroup>(new[] { source1 });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.GatherPreview();

				Assert.AreEqual(1, source1.NewFileCount);
				Assert.AreEqual(0, source1.UpdateFileCount);
				Assert.AreEqual(0, source1.DeleteFileCount);
				Assert.AreEqual(9, source1.NetChangeInBytes);
			}
		}

		[TestAttribute]
		public void Run_EmptyDest_SingleFileIsBeCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var progress = new StringBuilderProgress() {ShowVerbose = true};

				var sync = new MirrorController(to.Path, groups, 100, progress);

				sync.Run();
				AssertFileExists(sync, source, to, "1.txt");
			}
		}

		[TestAttribute]
		public void Run_FileLocked_OtherFileCopied()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				System.IO.File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				System.IO.File.WriteAllText(from.Combine("test2.txt"), "Blah blah blah");
				System.IO.File.WriteAllText(from.Combine("test3.txt"), "Blah blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				var groups = new List<FileGroup>(new[] { source });
				var progress = new StringBuilderProgress(){ShowVerbose=true};
				var controller = new MirrorController(to.Path, groups, 100, progress);

				using(File.OpenWrite(from.Combine("test2.txt")))//lock it up
				{
					controller.Run();
				}
				AssertFileExists(controller, source, to, "test1.txt");
				AssertFileExists(controller, source, to, "test3.txt");
				AssertFileDoesNotExist(controller, source, to, "test2.txt");
				Assert.That(progress.ErrorEncountered);
			}
		}

		[Test]
		public void Run_GroupHasDoDeletePolicy_DeletionIsPropagated()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null) {NormallyPropogateDeletions = true};
				var groups = new List<FileGroup>(new[] { source });
				var controller = new MirrorController(to.Path, groups, 100, new NullProgress());
				controller.Run();

				//should be there at the destination
				AssertFileExists(controller, source, to, "test1.txt");
				File.Delete(from.Combine("test1.txt"));

				File.WriteAllText(from.Combine("test2.txt"), "Blah blah");
				controller = new MirrorController(to.Path, groups, 100, new NullProgress());
				controller.Run();

				AssertFileDoesNotExist(controller, source, to, "test1.txt");
			}
		}
		
		[Test]
		public void Run_FileRemovedAndGroupHasDefaultDeletePolicy_FileIsDeletedFromDest()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				File.WriteAllText(from.Combine("test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				
				//ensure this is the defualt
				Assert.IsFalse(source.NormallyPropogateDeletions);

				var groups = new List<FileGroup>(new[] { source });
				var sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				sync.Run();
				string destFile = to.Combine(sync.DestinationRootForThisUser, source.Name, "test1.txt");
				Assert.IsTrue(File.Exists(destFile));
				File.Delete(from.Combine("test1.txt"));

				sync = new MirrorController(to.Path, groups, 100, new NullProgress());
				Assert.IsTrue(File.Exists(destFile));
				sync.Run();

				Assert.IsFalse(File.Exists(to.Combine("test1.txt")));
			}
		}

		[Test]
		public void Run_FileInMercurialFolderRemoved_FileGetsDeletedFromDest()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				Directory.CreateDirectory(from.Combine(".hg"));
				File.WriteAllText(from.Combine(".hg","test1.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				//the key here is that even though the group calls for NO deletion,
				//we do it anyways inside of the mercurial folder (.hg)
				source.NormallyPropogateDeletions = false;

				var groups = new List<FileGroup>(new[] { source });
				var controller = new MirrorController(to.Path, groups, 100, new NullProgress());
				controller.Run();
				AssertFileExists(controller, source, to, Path.Combine(".hg", "test1.txt"));

				File.Delete(from.Combine(".hg","test1.txt"));
				controller = new MirrorController(to.Path, groups, 100, new NullProgress());
				controller.Run();

				AssertFileDoesNotExist(controller,source,to, Path.Combine(".hg", "test1.txt"));
			}
		}


		[TestAttribute]
		public void Run_FolderExcluded_IsSkipped()
		{
			using (var from = new TemporaryFolder("synctest_source"))
			using (var to = new TemporaryFolder("synctest_dest"))
			{
				Directory.CreateDirectory(from.Combine("sub"));
				File.WriteAllText(from.Combine("sub", "one.txt"), "Blah blah");
				var source = new RawDirectoryGroup("1", from.Path, null, null);
				source.Filter.SubdirectoryExcludes.Add("sub");

				var groups = new List<FileGroup>(new[] { source });
				var progress = new StringBuilderProgress() { ShowVerbose = true };
				var sync = new MirrorController(to.Path, groups, 100, progress);
				sync.Run();

				// we don't get this progress yet Assert.That(progress.Text.ToLower().Contains("skip"));
				Assert.AreEqual(0, source.NewFileCount);
				Assert.AreEqual(0, source.UpdateFileCount);
				Assert.AreEqual(0, source.DeleteFileCount);
			}
		}

        [TestAttribute]
        public void Run_FileExtenstionExcluded_IsSkipped()
        {
            using (var from = new TemporaryFolder("synctest_source"))
            using (var to = new TemporaryFolder("synctest_dest"))
            {
                 File.WriteAllText(from.Combine("one.txt"), "Blah blah");
                var source = new RawDirectoryGroup("1", from.Path, null, null);
                source.Filter.FileNameExcludes.Add("*.txt");

                var groups = new List<FileGroup>(new[] { source });
                var progress = new StringBuilderProgress() { ShowVerbose = true };
                var sync = new MirrorController(to.Path, groups, 100, progress);
                sync.Run();

                AssertFileDoesNotExist(sync, source, to, "one.txt");
                // we don't get this progress yet Assert.That(progress.Text.ToLower().Contains("skip"));
                Assert.AreEqual(0, source.NewFileCount);
                Assert.AreEqual(0, source.UpdateFileCount);
                Assert.AreEqual(0, source.DeleteFileCount);
            }
        }


        [Test]
        public void Run_FileIncludedInPreviousGroup_NotCopiedAgainLater()
        {
            using (var from = new TemporaryFolder("synctest_source"))
            using (var to = new TemporaryFolder("synctest_dest"))
            {
                File.WriteAllText(from.Combine("one.txt"), "Blah blah");
                var source1 = new RawDirectoryGroup("1", from.Path, null, null);


                var source2 = new RawDirectoryGroup("2", from.Path, null, null);
                var groups = new List<FileGroup>(new[] { source1, source2 });
                var sync = new MirrorController(to.Path, groups, 100, new ConsoleProgress());
                sync.Run();
                AssertFileExists(sync, source1, to, "one.txt");
                AssertFileDoesNotExist(sync, source2, to, "one.txt");
            }
        }


        [Test]
        public void Run_FileExcludedByPreviousGroup_ButIsCopiedByLaterGroup()
        {
            using (var from = new TemporaryFolder("synctest_source"))
            using (var to = new TemporaryFolder("synctest_dest"))
            {
                File.WriteAllText(from.Combine("one.txt"), "Blah blah");
                var source1 = new RawDirectoryGroup("1", from.Path, null, null);
                source1.Filter.FileNameExcludes.Add("*.txt");
                var source2 = new RawDirectoryGroup("2", from.Path, null, null);
                var groups = new List<FileGroup>(new[] { source1, source2 });
                var sync = new MirrorController(to.Path, groups, 100, new ConsoleProgress());
                sync.Run();
                AssertFileExists(sync, source2, to, "one.txt");
                AssertFileDoesNotExist(sync, source1, to, "one.txt");
            }
        }
        
        [Test]
        public void Run_ToldToSkipExtensionButSourceHasUpperCaseOfExt_DoesNotCopy()
        {
            using (var from = new TemporaryFolder("synctest_source"))
            using (var to = new TemporaryFolder("synctest_dest"))
            {
                File.WriteAllText(from.Combine("one.TXT"), "Blah blah");
                var source = new RawDirectoryGroup("1", from.Path, null, null);
                source.Filter.FileNameExcludes.Add("*.txt");

                var groups = new List<FileGroup>(new[] { source });
                var progress = new StringBuilderProgress() { ShowVerbose = true };
                var sync = new MirrorController(to.Path, groups, 100, progress);
                sync.Run();

                AssertFileDoesNotExist(sync, source, to, "one.txt");
                // we don't get this progress yet Assert.That(progress.Text.ToLower().Contains("skip"));
                Assert.AreEqual(0, source.NewFileCount);
                Assert.AreEqual(0, source.UpdateFileCount);
                Assert.AreEqual(0, source.DeleteFileCount);
            }
        }

        /// <summary>
        /// regression, where "07Support&F 7f3"  lead to a crash as MirrorController tried to do a string.format() with that
        /// </summary>
	    [Test]
	    public void Run_PathHasDangerousCharacters_DoesCopy()
	    {
            //couldn't make it break on what came to me over email: var problemPart = "07Support&F 7f3";
            var problemPart = "{9}";
            using (var from = new TemporaryFolder("synctest_source"))
            using (var to = new TemporaryFolder("synctest_dest"))
            {
                using (var sub =  new TemporaryFolder(from, problemPart))
                {
                    var fileName = "1.txt";
                    System.IO.File.WriteAllText(sub.Combine(fileName), "Blah blah");
                    var source = new RawDirectoryGroup("1", from.Path, null, null);
                    var groups = new List<FileGroup>(new[] { source });
                    var progress = new ConsoleProgress() { ShowVerbose = true };

                    var sync = new MirrorController(to.Path, groups, 100, progress);

                    sync.Run();
                    string path = to.Combine ("1",problemPart,fileName);
			        Assert.That(File.Exists(path),  path);
                }
            }
	    }

        /// <summary>
        /// regression, related to dotnet 3.5's short path limit (around 260)
        /// </summary>
        [Test, Ignore("not yet")]
        public void Run_PathIsVeryLong_DoesCopy()
        {
            //we're adding this guid because teh normal tempfolder code can't handle this super deap thing, can't clear it out for us
            //as we re-run this test
            var guid = Guid.NewGuid().ToString();
            using (var from = new TemporaryFolder("synctest_source"+guid))
            using (var to = new TemporaryFolder("synctest_dest"+guid))
            {
                string sub = from.Path;
                for (int i = 0; i < 10; i++)
                {
                    sub = sub + Path.DirectorySeparatorChar+"OnePartOfTheDirectory" + i;
                    Microsoft.Experimental.IO.LongPathDirectory.Create(sub);
                }
                
                var fileName = "1.txt";
                using(var file = Microsoft.Experimental.IO.LongPathFile.Open(sub.CombineForPath(fileName), FileMode.CreateNew,
                                                            FileAccess.ReadWrite))
                {
                }
                var source = new RawDirectoryGroup("Group1", from.Path, null, null);
                var groups = new List<FileGroup>(new[] {source});
                var progress = new ConsoleProgress() {ShowVerbose = true};

                var sync = new MirrorController(to.Path, groups, 100, progress);

                sync.Run();
                string path = sub.Replace("synctest_source"+guid, "synctest_dest"+guid+Path.DirectorySeparatorChar+"Group1").CombineForPath("1.txt");

                Assert.That(LongPathFile.Exists(path), path);
            }
        }

	    private void AssertFileDoesNotExist(MirrorController controller, RawDirectoryGroup source, TemporaryFolder destFolder, string fileName)
		{
			string path = destFolder.Combine(controller.DestinationRootForThisUser, source.Name, fileName);
			Assert.IsFalse(File.Exists(path), path);
		}
		private void AssertFileExists(MirrorController controller, RawDirectoryGroup source, TemporaryFolder destFolder, string fileName)
		{
			string path = destFolder.Combine(controller.DestinationRootForThisUser, source.Name, fileName);
			Assert.IsTrue(File.Exists(path), path);
		}

	}
}
