// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using Xunit;

namespace Asmichi.Utilities.ProcessManagement
{
    public class ProcessPipelineStartInfoTest
    {
        [Fact]
        public void DefaultValueTest()
        {
            var sut = new ProcessPipelineStartInfo();

            Assert.Equal(InputRedirection.NullDevice, sut.StdInputRedirection);
            Assert.Equal(OutputRedirection.ParentOutput, sut.StdOutputRedirection);
            Assert.Equal(OutputRedirection.ParentError, sut.StdErrorRedirection);
            Assert.Null(sut.StdInputFile);
            Assert.Null(sut.StdInputHandle);
            Assert.Null(sut.StdOutputFile);
            Assert.Null(sut.StdOutputHandle);
            Assert.Null(sut.StdErrorFile);
            Assert.Null(sut.StdErrorHandle);
        }

        [Fact]
        public void ItemDefaultValueTest()
        {
            var sut = new ProcessPipelineItem();

            Assert.Null(sut.FileName);
            Assert.Null(sut.Arguments);
            Assert.Null(sut.WorkingDirectory);
            Assert.Null(sut.EnvironmentVariables);
            Assert.Equal(ProcessPipelineItemFlags.RedirectStandardOutput, sut.Flags);
        }
    }
}
