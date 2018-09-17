// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Asmichi.Utilities.Utilities
{
    public class CommandLineUtilTest
    {
        [Fact]
        public void MakeCommandLineQuotesArguments()
        {
            // NOTE: in the following asserts, we substitute ' for " to ease escaping.

            // no need for quoting
            assert("cmd 1 2 3", "cmd", "1", "2", "3");
            assert(@"c\m\d\ \1\2\3\", @"c\m\d\", @"\1\2\3\");

            // spaces, tabs
            assert(@"'c m d' '1 2' a", "c m d", "1 2", "a");
            assert("'c\tm\td' '1\t2' a", "c\tm\td", "1\t2", "a");
            assert(@"'c m d' ' 1 2 ' a", "c m d", " 1 2 ", "a");
            assert("'c\tm\td' '\t1\t2\t' a", "c\tm\td", "\t1\t2\t", "a");

            // quotes
            assert(@"'\'cmd\'' '\'1\''", "'cmd'", "'1'");

            // backslashes in a quoted part (no need for escape)
            assert(@"'c m\d' '1 2\3'", @"c m\d", @"1 2\3");

            // backslashes followed by a double quote
            assert(@"'cmd\\\'' '123\\\''", @"cmd\'", @"123\'");
            assert(@"'cmd\\\\\'' '123\\\\\''", @"cmd\\'", @"123\\'");

            void assert(string expected, string fileName, params string[] args)
            {
                string replace(string s) => s.Replace('\'', '"');

                Assert.Equal(
                    replace(expected),
                    CommandLineUtil.MakeCommandLine(replace(fileName), args.Select(replace).ToArray()).ToString());
            }
        }
    }
}
