using Rhino.Geometry;
using Rhino.Input;
using System.Collections.Generic;

namespace IDS.Core.Utilities
{
    /**
     * Custom GetPoint class for picking points of a curve on a surface.
     * The curve is drawn dynamically as the mouse is moved.
     */

    public class GetCurvePoints : Rhino.Input.Custom.GetPoint
    {
        /** Default constructor, always called */

        public GetCurvePoints()
        {
            ShouldValidatePoints = false;
        }

        /**
         * Get a new curve point.
         *
         * @return      GetResult.Point if a point was indicated,
         *              GetResult.Miss if an invalid point was indicated.
         */

        public new GetResult Get()
        {
            base.EnableTransparentCommands(false);
            GetResult gottype = base.Get();
            if (gottype == GetResult.Point)
            {
                if (ShouldValidatePoints && !IsValidCurvePoint(this.Point()))
                {
                    return GetResult.Miss;
                }
            }
            return gottype;
        }

        ///////////////////////////////////////////////////////////////////////
        // Dynamic constraint checking //
        ///////////////////////////////////////////////////////////////////////

        /**
         * Should constraints be checked and reflected in the
         * screw drawing color or not.
         */

        public bool ShouldValidatePoints
        {
            get;
            set;
        }

        /**
         * Check if screw constraints are satisfied
         *
         * @return      True if satisfied, false otherwise.
         */

        protected virtual bool IsValidCurvePoint(Point3d curvePoint)
        {
            foreach (CurvePointValidator validator in _curve_point_validators)
            {
                if (!validator(curvePoint))
                    return false;
            }
            return true;
        }

        

        /**
         * Add a dynamic constraint checking function to be executed during
         * each dynamic update of the screw paramers.
         */

        public void AddDynamicPointValidator(CurvePointValidator checker)
        {
            _curve_point_validators.Add(checker);
        }

        /** List of assigned constraint checking functions */
        protected List<CurvePointValidator> _curve_point_validators = new List<CurvePointValidator>();
    }
}