using IDS.Core.ImplantDirector;

namespace IDS.Testing.UnitTests
{
    public class GleniusImplantDirectorMock : ICaseInfoProvider
    {
        public int draft { get; set; }
        public string caseId { get; set; }
        public int version { get; set; }
        public bool defectIsLeft { get; }
    }
}
