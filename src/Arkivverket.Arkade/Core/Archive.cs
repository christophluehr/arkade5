using System.IO;
using Arkivverket.Arkade.Util;

namespace Arkivverket.Arkade.Core
{
    public class Archive
    {
        public Uuid Uuid { get; private set; }
        public WorkingDirectory WorkingDirectory { get; private set; }
        public ArchiveType ArchiveType { get; private set; }

        public Archive(ArchiveType archiveType, Uuid uuid, WorkingDirectory workingDirectory)
        {
            ArchiveType = archiveType;
            Uuid = uuid;
            WorkingDirectory = workingDirectory;
        }

        public string GetContentDescriptionFileName()
        {
            return WorkingDirectory.Content().WithFile(ArkadeConstants.ArkivstrukturXmlFileName).FullName;
        }

        public string GetStructureDescriptionFileName()
        {
            return WorkingDirectory.AdministrativeMetadata().WithFile(ArkadeConstants.AddmlXmlFileName).FullName;
        }

        public FileInfo GetInformationPackageFileName()
        {
            return WorkingDirectory.Root().WithFile(Uuid.ToString() + ".tar");
        }
    }

    public enum ArchiveType
    {
        Noark3,
        Noark4,
        Noark5,
        Fagsystem,
    }
}