namespace IDS.Glenius.Operations
{
    public class AnatomicalMeasurementsLoader
    {
        private readonly AnatomicalMeasurements anatomicalMeasurements;
        private readonly bool success;

        public AnatomicalMeasurementsLoader(string reconstructionParametersCSVPath, bool isLeft)
        {
            ReconstructionCSVReader reader = new ReconstructionCSVReader();
            if (reader.Read(reconstructionParametersCSVPath))
            {
                anatomicalMeasurements = new AnatomicalMeasurements(reader.angleInf, reader.trig, reader.glenPlaneOrigin, reader.glenPlaneNormal, isLeft);
                success = true;
            }
            else
            {
                success = false;
            }
        }

        public AnatomicalMeasurements GetAnatomicalMeasurements()
        {
            if(success)
            {
                return anatomicalMeasurements;
            }
            else
            {
                return null;
            }
        }
    }
}
