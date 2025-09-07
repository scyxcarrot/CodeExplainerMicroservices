using IDS.Interface.Geometry;
using System;
using System.Collections.Generic;

namespace IDS.Interface.Loader
{
    public interface IPreopLoader
    {
        List<IPreopLoadResult> PreLoadPreop();

        void CleanUp();

        List<IPreopLoadResult> ImportPreop();

        bool GetPlanes(out IPlane sagittalPlane, out IPlane axialPlane, out IPlane coronalPlane, out IPlane midSagittalPlane);

        List<Tuple<string, bool>> GetPartInfos();

        bool ExportPreopToStl(List<string> partNames, string outputDirectory);

        /// <summary>
        /// Get osteotomy handler information
        /// </summary>
        /// <param name="osteotomyHandler">Osteotomy handler information</param>
        /// <returns>True if successfully retrieve the information</returns>
        bool GetOsteotomyHandler(out List<IOsteotomyHandler> osteotomyHandler);
    }
}
