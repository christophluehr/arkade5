using System;
using System.IO;
using Arkivverket.Arkade.Resources;

namespace Arkivverket.Arkade.Core
{
    public class ArchiveContentReader : IArchiveContentReader
    {
        public Stream GetContentAsStream(Archive archive)
        {
            string fileName = archive.GetContentDescriptionFileName();
            return GetFileAsStream(fileName);
        }

        public Stream GetStructureContentAsStream(Archive archive)
        {
            string fileName = archive.GetStructureDescriptionFileName();
            return GetFileAsStream(fileName);
        }

        private static Stream GetFileAsStream(string fileName)
        {
            try
            {
                return File.OpenRead(fileName);
            }
            catch (Exception e)
            {
                string message = string.Format(Messages.FileNotFoundMessage, fileName);
                throw new ArkadeException(message, e);
            }
        }
    }
}