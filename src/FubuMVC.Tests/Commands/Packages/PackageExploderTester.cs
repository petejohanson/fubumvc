using System;
using System.IO;
using AssemblyPackage;
using Fubu.Packages;
using FubuCore;
using FubuMVC.Core;
using FubuMVC.Core.Packaging;
using NUnit.Framework;
using Rhino.Mocks;
using System.Linq;
using System.Collections.Generic;

namespace FubuMVC.Tests.Commands.Packages
{
    [TestFixture]
    public class integration_test_of_exploding_an_assembly_package
    {
        private PackageFiles theFiles;

        [SetUp]
        public void SetUp()
        {
            var exploder = new PackageExploder(new ZipFileService(),
                                               new PackageExploderLogger(s => Console.WriteLine(s)), new FileSystem());

            theFiles = new PackageFiles();
            exploder.ExplodeAssembly("app1", typeof(AssemblyPackageMarker).Assembly, theFiles);
        }

        [Test]
        public void can_retrieve_data_from_package()
        {
            var text = "not the right thing";
            theFiles.ForData("1.txt", (name, data) =>
            {
                name.ShouldEqual("1.txt");
                text = new StreamReader(data).ReadToEnd();
            });

            // The text of this file in the AssemblyPackage data is just "1"
            text.ShouldEqual("1");
        }

        [Test]
        public void can_retrieve_web_content_folder_from_package()
        {
            var expected = "not this";
            theFiles.ForFolder(FubuMvcPackages.WebContentFolder, folder =>
            {
                expected = folder;
            });

            expected.ShouldEqual("app1\\bin\\fubu-packages\\AssemblyPackage\\WebContent".ToFullPath());
        }
    }


    [TestFixture]
    public class when_extracting_package_zip_files : PackageExploderContext
    {
        [Test]
        public void is_package_zip_positive()
        {
            PackageExploder.IsEmbeddedPackageZipFile("a.b.c.pak-webcontent.zip").ShouldBeTrue();
        }

        [Test]
        public void is_package_zip_negative_1()
        {
            PackageExploder.IsEmbeddedPackageZipFile("a.b.c.pak-webcontent.txt").ShouldBeFalse();
        }

        [Test]
        public void is_package_zip_negative_2()
        {
            PackageExploder.IsEmbeddedPackageZipFile("a.b.c.webcontent.zip").ShouldBeFalse();
        }

        [Test]
        public void get_package_folder_name()
        {
            PackageExploder.EmbeddedPackageFolderName("a.b.c.pak-webcontent.zip").ShouldEndWith("webcontent");
            PackageExploder.EmbeddedPackageFolderName("a.b.c.pak-data.zip").ShouldEndWith("data");
        }
    }

    [TestFixture]
    public class when_cleaning_all_the_packages : PackageExploderContext
    {
        protected override void beforeEach()
        {
            thePackagesAlreadyExplodedAre("pak1", "pak2", "pak3");
            ClassUnderTest.CleanAll(theApplicationDirectory);
        }

        [Test]
        public void should_clean_all_existing_package_folders()
        {
            assertPackageFolderWasDeleted("pak1");
            assertPackageFolderWasDeleted("pak2");
            assertPackageFolderWasDeleted("pak3");
        }
    }

    [TestFixture]
    public class when_reading_the_version_from_a_package_folder : PackageExploderContext
    {
        [Test]
        public void the_version_file_exists()
        {
            var guid = Guid.NewGuid();
            theExistingVersionIs("pak1", guid);

            string directory = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                      "pak1");

            ClassUnderTest.ReadVersion(directory).ShouldEqual(guid);
        }

        [Test]
        public void the_version_file_does_not_exist_so_return_empty()
        {
            theExistingVersionDoesNotExist("pak1");
            string directory = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                      "pak1");

            ClassUnderTest.ReadVersion(directory).ShouldEqual(Guid.Empty);
        }
    }

    [TestFixture]
    public class explode_all_with_zip_files_and_no_unzipped_directories : PackageExploderContext
    {
        protected override void beforeEach()
        {
            theZipFilesAre("pak1", "pak2", "pak3");

            // No packages are already exploded
            thePackagesAlreadyExplodedAre();

            ClassUnderTest.ExplodeAllZipsAndReturnPackageDirectories(theApplicationDirectory);
        }

        [Test]
        public void should_have_exploded_all_three_packages()
        {
            assertZipFileWasExploded("pak1");
            assertZipFileWasExploded("pak2");
            assertZipFileWasExploded("pak3");
        }
    }

    [TestFixture]
    public class explode_a_zip_file_if_the_version_is_different_than_the_existing_package_folder : PackageExploderContext
    {
        private Guid zipGuid;
        private Guid folderGuid;

        protected override void beforeEach()
        {
            theZipFilesAre("pak1");
            zipGuid = Guid.NewGuid();
            folderGuid = Guid.NewGuid();
            thePackagesAlreadyExplodedAre("pak1");
        
            theExistingVersionIs("pak1", folderGuid);
            theZipVersionIs("pak1", zipGuid);

            ClassUnderTest.ExplodeAllZipsAndReturnPackageDirectories(theApplicationDirectory);
        }

        [Test]
        public void should_delete_the_old_contents()
        {
            assertPackageFolderWasDeleted("pak1");
        }

        [Test]
        public void should_extract_the_zip_file()
        {
            assertZipFileWasExploded("pak1");
        }

    }


    [TestFixture]
    public class explode_a_zip_file_if_the_version_is_missing_from_the_existing_package_folder : PackageExploderContext
    {
        private Guid zipGuid;

        protected override void beforeEach()
        {
            theZipFilesAre("pak1");
            zipGuid = Guid.NewGuid();
            thePackagesAlreadyExplodedAre("pak1");

            theExistingVersionDoesNotExist("pak1");
            theZipVersionIs("pak1", zipGuid);

            ClassUnderTest.ExplodeAllZipsAndReturnPackageDirectories(theApplicationDirectory);
        }

        [Test]
        public void should_delete_the_old_contents()
        {
            assertPackageFolderWasDeleted("pak1");
        }

        [Test]
        public void should_extract_the_zip_file()
        {
            assertZipFileWasExploded("pak1");
        }

    }

    [TestFixture]
    public class do_not_explode_a_zip_file_if_the_version_is_the_same_as_the_existing_folder : PackageExploderContext
    {
        private Guid theSameGuid;

        protected override void beforeEach()
        {
            theZipFilesAre("pak1");
            theSameGuid = Guid.NewGuid();
            thePackagesAlreadyExplodedAre("pak1");

            theExistingVersionIs("pak1", theSameGuid);
            theZipVersionIs("pak1", theSameGuid);

            ClassUnderTest.ExplodeAllZipsAndReturnPackageDirectories(theApplicationDirectory);
        }

        [Test]
        public void should_NOT_delete_the_old_contents()
        {
            assertPackageFolderWasNotDeleted("pak1");
        }

        [Test]
        public void should_NOT_extract_the_zip_file()
        {
            assertZipFileWasNotExploded("pak1");
        }
    }


    public abstract class PackageExploderContext : InteractionContext<PackageExploder>
    {
        protected readonly string theApplicationDirectory = "c:\\app";

        protected void assertZipFileWasExploded(string packageName)
        {
            var fileName = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                              packageName + ".zip");

            var directoryName = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                              packageName);

            MockFor<IZipFileService>().AssertWasCalled(x => x.ExtractTo(fileName, directoryName));
            
        }

        protected void assertZipFileWasNotExploded(string packageName)
        {
            var fileName = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                              packageName + ".zip");

            var directoryName = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                              packageName);

            MockFor<IZipFileService>().AssertWasNotCalled(x => x.ExtractTo(fileName, directoryName));

        }

        protected void theZipFilesAre(params string[] packageNames)
        {
            var zipFiles =
                packageNames.Select(
                    x =>
                    FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder, x + ".zip"));

            MockFor<IFileSystem>().Stub(x => x.FileNamesFor(new FileSet(){
                Include = "*.zip"
            }, theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder)).Return(zipFiles);
        }

        protected void theZipVersionIs(string packageName, Guid version)
        {
            var file = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder, packageName + ".zip");
            MockFor<IZipFileService>().Stub(x => x.GetVersion(file)).Return(version.ToString());
        }

        protected void thePackagesAlreadyExplodedAre(params string[] packageNames)
        {
            var directories = packageNames.Select(x =>
            {
                return FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder, x);
            });

            directories.Each(dir =>
            {
                MockFor<IFileSystem>().Stub(x => x.DirectoryExists(dir)).Return(true);
            });

            MockFor<IFileSystem>().Stub(x => x.ChildDirectoriesFor(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder))
                .Return(directories);

        }

        protected void assertPackageFolderWasDeleted(string packageName)
        {
            var packageDirectory = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                                      packageName);
            MockFor<IFileSystem>().AssertWasCalled(x => x.DeleteDirectory(packageDirectory));
        }

        protected void assertPackageFolderWasNotDeleted(string packageName)
        {
            var packageDirectory = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                                      packageName);
            MockFor<IFileSystem>().AssertWasNotCalled(x => x.DeleteDirectory(packageDirectory));
        }

        protected void theExistingVersionIs(string packageName, Guid guid)
        {

            string directory = FileSystem.Combine(theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder,
                                                  packageName);

            MockFor<IFileSystem>().Stub(x => x.FileExists(directory, FubuMvcPackages.VersionFile)).Return(true);
            MockFor<IFileSystem>().Stub(x => x.ReadStringFromFile(directory, FubuMvcPackages.VersionFile)).Return(guid.ToString());
        }

        protected void theExistingVersionDoesNotExist(string packageName)
        {
            var pathParts = new string[]{theApplicationDirectory, "bin", FubuMvcPackages.FubuPackagesFolder, packageName,
                             FubuMvcPackages.VersionFile};

            
            MockFor<IFileSystem>().Stub(x => x.FileExists(pathParts)).Return(false);
        }


    }
}