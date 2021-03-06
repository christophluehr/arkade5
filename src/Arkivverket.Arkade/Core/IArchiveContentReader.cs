﻿using System.IO;

namespace Arkivverket.Arkade.Core
{
    public interface IArchiveContentReader
    {
        Stream GetContentAsStream(Archive archive);
        Stream GetStructureContentAsStream(Archive archive);
    }
}