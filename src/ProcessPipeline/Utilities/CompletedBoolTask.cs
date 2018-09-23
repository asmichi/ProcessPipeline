// Copyright 2018 @asmichi (at github). Licensed under the MIT License. See LICENCE in the project root for details.

using System.Threading.Tasks;

namespace Asmichi.Utilities.Utilities
{
    // Cached completed Task<bool>
    internal static class CompletedBoolTask
    {
        public static readonly Task<bool> True = Task.FromResult(true);
    }
}
