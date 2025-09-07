namespace IDS.CMFImplantCreation.Configurations
{
    internal interface IConfiguration
    {
        PastilleConfiguration GetPastilleConfiguration(string screwType);

        OverallImplantParams GetOverallImplantParameter();

        IndividualImplantParams GetIndividualImplantParameter();

        LandmarkImplantParams GetLandmarkImplantParameter();
    }
}
