using IDS.Core.ImplantDirector;
using System.Linq;
using System.Text;

namespace IDS.Glenius.Operations
{
    public class ImplantFileNameGenerator
    {
        private readonly ICaseInfoProvider caseInfoProvider;

        public bool AddExtension { get; set; }

        private string _extension;
        public string Extension
        {
            get
            {
                return _extension;
            }
            set
            {
                if (value.Any())
                {
                    _extension = value;
                    AddExtension = true;
                }
                else
                {
                    AddExtension = false;
                }
            }
        }

        public ImplantFileNameGenerator(ICaseInfoProvider caseInfoProvider)
        {
            this.caseInfoProvider = caseInfoProvider;
            _extension = "";
            AddExtension = false;
        }

        public string GenerateFileName(string entityName)
        {
            var name = $"{caseInfoProvider.caseId}_{entityName}_v{caseInfoProvider.version}_draft{caseInfoProvider.draft}";

            if (!AddExtension)
            {
                return name;
            }

            StringBuilder builder = new StringBuilder(name);
            builder.AppendFormat(".{0}", _extension);
            return builder.ToString();
        }

        public string GeneratePlateForReportingFileName()
        {
            return GenerateFileName("Plate_ForReporting");
        }

        public string GeneratePlateForFinalizationFileName()
        {
            return GenerateFileName("Plate_ForFinalization");
        }

        public string GeneratePlateForProductionOffsetFileName()
        {
            return GenerateFileName("Plate_ForProductionOffset");
        }

        public string GeneratePlateForProductionFileName()
        {
            return GenerateFileName("Plate_ForProduction");
        }

        public string GenerateScaffoldForReportingFileName()
        {
            return GenerateFileName("Scaffold_ForReporting");
        }

        public string GenerateScaffoldForFinalizationFileName()
        {
            return GenerateFileName("Scaffold_ForFinalization");
        }
    }
}
