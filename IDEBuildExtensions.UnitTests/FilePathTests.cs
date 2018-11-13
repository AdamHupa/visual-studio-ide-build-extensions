using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

using IDEBuildExtensions;

namespace IDEBuildExtensions.UnitTests
{
    [TestClass]
    public class FilePathTests
    {
        [TestMethod]
        public void Validate_FilePath()
        {
            System.Collections.Generic.List<Tuple<bool, string>> testPathSet = new System.Collections.Generic.List<Tuple<bool, string>>()
            {
                new Tuple<bool, string>(true, @"obj\Debug\IDETestLibrary.dll.config"),
                new Tuple<bool, string>(true, @"\\server\share\path\"),
                new Tuple<bool, string>(true, @"\\server\share\path\file.txt"),
                new Tuple<bool, string>(true, @"C:\Users\user_name\Desktop\Unified Work Solution\IDE Build Extensions\IDETestLibrary\obj\Debug\"),
                new Tuple<bool, string>(true, @"C:\Users\user_name\Desktop\Unified Work Solution\IDE Build Extensions\IDETestLibrary\obj\Debug\IDETestLibrary.dll.config"),
                
                new Tuple<bool, string>(true,  @"C:\Users\user_name\Desktop\Regex File Path Validation\" + new string('s', 193) + @"\"),
                new Tuple<bool, string>(false, @"C:\Users\user_name\Desktop\Regex File Path Validation\" + new string('s', 193) + @"S_248plus\"),
                new Tuple<bool, string>(true,  @"C:\Users\user_name\Desktop\Regex File Path Validation\1" + new string('s', 187) + @"S\aaaaaaaaaaaa.txt"),
                new Tuple<bool, string>(false, @"C:\Users\user_name\Desktop\Regex File Path Validation\1" + new string('s', 187) + @"S\aaaaaaaaaaaa_260plus.txt"),
                new Tuple<bool, string>(true, @"C:\"),
                new Tuple<bool, string>(true, @"\\?\Volume{26a21bda-a627-11d7-9931-806b9f6e6963}\"),
                new Tuple<bool, string>(true, @"\\?\Volume{fda21bda-4277-a1d7-9abc-f06b9f6e6963}\"),
                new Tuple<bool, string>(true, @"\\?\C:\Users\user_name\Desktop\"),
                new Tuple<bool, string>(true, @"\\?\C:\Users\user_name\Desktop\notes.txt"),
                
                new Tuple<bool, string>(true,  @"C:\Users\user_name\Desktop\notes.txt"),
                new Tuple<bool, string>(false, @"C:\Users\user_name\\Desktop\notes.txt"),
                new Tuple<bool, string>(false, @"C:\Users\user_name\/Desktop\notes.txt"),
                new Tuple<bool, string>(false, @"C:\Users\user_name/\\Desktop\notes.txt"),
                new Tuple<bool, string>(false, @"C:\Users\user_name\\Desktop\notes.txt"),

                new Tuple<bool, string>(false, @"AUX"),
                new Tuple<bool, string>(true,  @"AUXa"),
                new Tuple<bool, string>(false, @"AUX\"),
                new Tuple<bool, string>(true,  @"AUXa\"),
                new Tuple<bool, string>(false, @"C:\AUX"),
                new Tuple<bool, string>(true,  @"C:\AUXa"),
                new Tuple<bool, string>(false, @"C:\AUX\"),
                new Tuple<bool, string>(true,  @"C:\AUXa\"),
                
                new Tuple<bool, string>(false, @"C:\ path\path\"),
                new Tuple<bool, string>(false, @"C:\.path\path\"),
                new Tuple<bool, string>(false, @"C:\path \path\"),
                new Tuple<bool, string>(false, @"C:\path.\path\"),

                new Tuple<bool, string>(false, @"C:\ file"),
                new Tuple<bool, string>(true,  @"C:\.file"),
                new Tuple<bool, string>(false, @"C:\file "),
                new Tuple<bool, string>(false, @"C:\file."),

                //new Tuple<bool, string>(, @""),
            };

            foreach (var testEntry in testPathSet)
            {
                Assert.IsTrue(testEntry.Item1 == FilePath.Validate(testEntry.Item2),
                              String.Format("Path validation should {0} for:\n{1}\n", (testEntry.Item1 ? "succeed" : "fail"), testEntry.Item2));
            }
        }
    }
}
