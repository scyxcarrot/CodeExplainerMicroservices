using IDS.CMF.DataModel;
using IDS.Core.V2.Geometries;
using IDS.RhinoInterfaces.Converter;

namespace IDS.CMF.TestLib.Components
{
    public class MedicalCoordinateSystemComponent
    {
        public IDSPlane SagittalPlane { get; set; } = IDSPlane.Zero;

        public IDSPlane AxialPlane { get; set; } = IDSPlane.Zero;

        public IDSPlane CoronalPlane { get; set; } = IDSPlane.Zero;

        public IDSPlane MidSagittalPlane { get; set; } = IDSPlane.Zero;

        public void ParseToDirector(CMFImplantDirector director)
        {
            var sagittalPlane = SagittalPlane.ToRhinoPlane();
            var axialPlane = AxialPlane.ToRhinoPlane();
            var coronalPlane = CoronalPlane.ToRhinoPlane();
            var midSagittalPlane = Rhino.Geometry.Plane.Unset;
            //for backward compatibility: either for old 3dm or old config.json 
            if (!MidSagittalPlane.EpsilonEquals(IDSPlane.Zero, 0.0001) && !MidSagittalPlane.EpsilonEquals(IDSPlane.Unset, 0.0001))
            {
                midSagittalPlane = MidSagittalPlane.ToRhinoPlane();
            }
            director.MedicalCoordinateSystem = new MedicalCoordinateSystem(sagittalPlane, axialPlane, coronalPlane, midSagittalPlane);
        }

        public void FillToComponent(CMFImplantDirector director)
        {
            SagittalPlane = director.MedicalCoordinateSystem.SagittalPlane.ToIDSPlane();
            AxialPlane = director.MedicalCoordinateSystem.AxialPlane.ToIDSPlane();
            CoronalPlane = director.MedicalCoordinateSystem.CoronalPlane.ToIDSPlane();
            MidSagittalPlane = director.MedicalCoordinateSystem.MidSagittalPlane.ToIDSPlane();
        }
    }
}
