using IDS.Core.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace IDS.Core.Fea
{
    public class Frd
    {
        private readonly string _frdFile;

        /// <summary>
        /// Gets the stress tensors.
        /// </summary>
        /// <value>
        /// The stress tensors.
        /// </value>
        public List<StressTensor> StressTensors { get; set; }

        /// <summary>
        /// Gets the displacements.
        /// </summary>
        /// <value>
        /// The displacements.
        /// </value>
        public List<double[]> Displacements { get; private set; }

        /// <summary>
        /// Gets the strains.
        /// </summary>
        /// <value>
        /// The strains.
        /// </value>
        public List<double[]> Strains { get; private set; }

        /// <summary>
        /// Gets the nodes.
        /// </summary>
        /// <value>
        /// The nodes.
        /// </value>
        public List<double[]> Nodes { get; set; }

        /// <summary>
        /// Gets the von mises stresses.
        /// </summary>
        /// <value>
        /// The von mises stresses.
        /// </value>
        public List<double> GetVonMisesStresses()
        {
            return StressTensors.Count == 0 ? new List<double>() : StressTensors.Select(x => x.vonMisesStress).ToList();
        }

        /// <summary>
        /// Gets the fatigue values.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <returns></returns>
        public List<double> GetFatigueValues(Material material)
        {
            var stresses = GetVonMisesStresses();

            return stresses.Select(stress => CalculateFatigue(stress, material)).ToList();
        }

        public static double CalculateFatigue(double stress, Material material)
        {
            var stressAmplitude = (stress - 0.1 * stress) / 2.0;
            var stressMean = (stress + 0.1 * stress) / 2.0;
            var fatigue = 1.0 / ((stressAmplitude / material.FatigueLimit) + (stressMean / material.UltimateTensileStrength) + 1e-6);
            return fatigue;
        }

        /// <summary>
        /// Gets the strain magnitudes.
        /// </summary>
        /// <returns></returns>
        /// <value>
        /// The von mises stresses.
        /// </value>
        public List<double> GetStrainMagnitudes()
        {
            return Strains.Count == 0 ? new List<double>() : Strains.Select(x => Math.Sqrt(Math.Pow(x[0],2) + Math.Pow(x[1], 2) + Math.Pow(x[2], 2))).ToList();
        }

        /// <summary>
        /// Gets the von mises stresses.
        /// </summary>
        /// <value>
        /// The von mises stresses.
        /// </value>
        public List<double> GetDisplacementMagnitudes()
        {
            return Displacements.Count == 0 ? new List<double>() : Displacements.Select(x => Math.Sqrt(Math.Pow(x[0], 2) + Math.Pow(x[1], 2) + Math.Pow(x[2], 2))).ToList();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Frd"/> class.
        /// </summary>
        public Frd()
        {
            StressTensors = new List<StressTensor>();
            Displacements = new List<double[]>();
            Strains = new List<double[]>();
            Nodes = new List<double[]>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Frd"/> class.
        /// </summary>
        /// <param name="frdFile">The FRD file.</param>
        public Frd(string frdFile) : this()
        {
            _frdFile = frdFile;
            Read();
        }

        /// <summary>
        /// Reads this instance.
        /// </summary>
        private void Read()
        {
            ReadStrains();
            ReadDisplacements();
            ReadStresses();
            ReadNodes();
        }

        private void ReadNodes()
        {
            Nodes = ReadArrays("2c");
        }

        /// <summary>
        /// Reads the strains.
        /// </summary>
        private void ReadStrains()
        {
            Strains = ReadArrays("tostrain");
        }

        /// <summary>
        /// Reads the displacements.
        /// </summary>
        private void ReadDisplacements()
        {
            Displacements = ReadArrays("disp");
        }

        /// <summary>
        /// Reads the stresses.
        /// </summary>
        private void ReadStresses()
        {
            var stressTensorArrays = ReadArrays("stress");
            StressTensors = new List<StressTensor>(); // clear
            foreach(var stressTensorArray in stressTensorArrays)
            {
                StressTensors.Add(new StressTensor(stressTensorArray[0],
                                                    stressTensorArray[1],
                                                    stressTensorArray[2],
                                                    stressTensorArray[3],
                                                    stressTensorArray[4],
                                                    stressTensorArray[5]));
            }
        }

        /// <summary>
        /// Reads the arrays.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns></returns>
        private List<double[]> ReadArrays(string tag)
        {
            var arrays = new List<double[]>();

            using (var sr = File.OpenText(_frdFile))
            {
                string line;
                var readNextValueLines = false;

                while ((line = sr.ReadLine()) != null)
                {
                    var trimmedLine = line.Trim();

                    if (trimmedLine.ToLower().Contains(tag.ToLower()))
                    {
                        readNextValueLines = true;
                    }
                    else if (readNextValueLines && trimmedLine.Length > 1 && trimmedLine.Substring(0, 2) == "-1")
                    {
                        arrays.Add(ReadLineAsValueArray(line));
                    }
                    else if (arrays.Count > 0)
                    {
                        readNextValueLines = false;
                    }
                }
            }

            return arrays;
        }

        /// <summary>
        /// Reads the line as value array.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private static double[] ReadLineAsValueArray(string line)
        {
            // Remove -1
            var parseLine = line.Substring(3, line.Length - 3);

            // Trim
            parseLine = parseLine.Trim();

            // Split
            var parts = parseLine.SplitBefore(new [] { ' ', '-', '+' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // Recombine
            var tensorElementsAsString = new List<string>();
            for (var i = 1; (i + 3) < parts.Count; i += 4)
            {
                tensorElementsAsString.Add(string.Join("", parts.GetRange(i, 4)));
            }

            // Parse as double
            var tensorElements = new List<double>(tensorElementsAsString.Count);
            tensorElements.AddRange(tensorElementsAsString.Select(tensorElementString => double.Parse(tensorElementString, NumberStyles.Float, CultureInfo.InvariantCulture)));

            return tensorElements.ToArray();
        }
    }
}