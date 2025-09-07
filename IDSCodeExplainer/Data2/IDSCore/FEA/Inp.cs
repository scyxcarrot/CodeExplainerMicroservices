using IDS.Core.Utilities;
using Rhino.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace IDS.Core.Fea
{
    public class Inp
    {
        /// <summary>
        /// The indices per line
        /// </summary>
        private const int IndicesPerLine = 16;

        /// <summary>
        /// Initializes a new instance of the <see cref="Inp"/> class.
        /// </summary>
        /// <param name="inpFile">The inp file.</param>
        public Inp(string inpFile) : this()
        {
            this.InpFile = inpFile;
        }

        /// <summary>
        /// Prevents a default instance of the <see cref="Inp"/> class from being created.
        /// </summary>
        public Inp()
        {
            InpFile = string.Empty;

            HeaderLines = new List<string>();
            Part = new InpPart();
            Simulation = new InpSimulation();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inp"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public Inp(ArchivableDictionary dictionary) : this()
        {
            var inpLines = new string[dictionary.Count];

            for (var i = 0; i < dictionary.Count; i++)
            {
                inpLines[i] = dictionary.GetString(i.ToString("D"));
            }

            Read(inpLines);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Inp"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="inpLines"></param>
        public Inp(string[] inpLines) : this()
        {
            Read(inpLines);
        }

        /// <summary>
        /// Gets or sets the header lines.
        /// </summary>
        /// <value>
        /// The header lines.
        /// </value>
        public List<string> HeaderLines { get; set; }

        /// <summary>
        /// Gets or sets the inp file.
        /// </summary>
        /// <value>
        /// The inp file.
        /// </value>
        public string InpFile { get; set; }

        /// <summary>
        /// Gets or sets the part.
        /// </summary>
        /// <value>
        /// The part.
        /// </value>
        public InpPart Part { get; set; }

        /// <summary>
        /// Gets or sets the simulation.
        /// </summary>
        /// <value>
        /// The simulation.
        /// </value>
        public InpSimulation Simulation { get; set; }

        /// <summary>
        /// Reads the specified inp lines.
        /// </summary>
        /// <param name="inpLines">The inp lines.</param>
        private void Read(string[] inpLines)
        {
            ReadHeader(inpLines);
            ReadNodes(inpLines);
            ReadElements(inpLines);
            ReadSimulationNSets(inpLines);
            ReadSimulationMaterial(inpLines);
            ReadSimulationBoundaryConditions(inpLines);
            ReadSimulationLoads(inpLines);
        }

        /// <summary>
        /// Reads the specified inp string.
        /// </summary>
        /// <param name="inpString">The inp string.</param>
        public void Read(string inpString)
        {
            var inpLines = inpString.Split('\n');
            Read(inpLines);
        }

        /// <summary>
        /// Reads the contents of the associated INP file.
        /// </summary>
        public void Read()
        {
            var inpLines = File.ReadAllLines(InpFile);
            Read(inpLines);
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder();

            WriteHeader(ref builder);
            WriteNodes(ref builder);
            WriteElements(ref builder);
            WriteNSets(ref builder, Simulation.NSetsBoundaryConditions);
            WriteNSets(ref builder, Simulation.NSetsLoad);
            WriteSimulationMaterial(ref builder);
            WriteSimulationBoundaryConditions(ref builder);
            WriteSimulationSolidSection(ref builder);
            WriteSimulationStepStart(ref builder);
            WriteSimulationStatic(ref builder);
            WriteSimulationLoads(ref builder);
            WriteSimulationOutputs(ref builder);
            WriteSimulationStepEnd(ref builder);

            var inpString = builder.ToString();
            return inpString;
        }

        /// <summary>
        /// To the strings.
        /// </summary>
        /// <returns></returns>
        public string[] ToStrings()
        {
            var inpString = this.ToString();
            return inpString.Split('\n');
        }

        /// <summary>
        /// To the archivable dictionary.
        /// </summary>
        /// <returns></returns>
        public ArchivableDictionary ToArchivableDictionary()
        {
            var dictionary = new ArchivableDictionary();

            var inpLines = this.ToStrings();

            for(var i = 0; i < inpLines.Count(); i++)
            {
                dictionary.Set(i.ToString("D"), inpLines[i]);
            }

            return dictionary;
        }

        /// <summary>
        /// Writes this instance.
        /// </summary>
        public void Write()
        {
            ClearInpFile();
            var inpString = this.ToString();

            File.WriteAllText(InpFile, inpString);
        }
        /// <summary>
        /// Checks if line identifies el set.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesElements(string line)
        {
            return CheckIfLineIdentifiesTag(line, "ELEMENT");
        }

        /// <summary>
        /// Checks if line identifies el set.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesElSet(string line)
        {
            return CheckIfLineIdentifiesTag(line, "ELSET");
        }

        /// <summary>
        /// Checks if line identifies header.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesHeader(string line)
        {
            return CheckIfLineIdentifiesTag(line, "HEADING");
        }

        /// <summary>
        /// Checks if line identifies nodes.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesNodes(string line)
        {
            return CheckIfLineIdentifiesTag(line, "NODE");
        }

        /// <summary>
        /// Checks if line identifies n set.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesNSet(string line)
        {
            return CheckIfLineIdentifiesTag(line, "NSET");
        }

        private bool CheckIfLineIdentifiesPartHeader(string line)
        {
            return CheckIfLineIdentifiesTag(line, "PART");
        }

        /// <summary>
        /// Checks if line identifies simulation boundary condition.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesSimulationBoundaryCondition(string line)
        {
            return CheckIfLineIdentifiesTag(line, "BOUNDARY");
        }

        /// <summary>
        /// Checks if line identifies simulation material.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesSimulationMaterial(string line)
        {
            return CheckIfLineIdentifiesTag(line, "MATERIAL");
        }

        /// <summary>
        /// Checks if line identifies simulation material elasticity.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesSimulationMaterialElasticity(string line)
        {
            return CheckIfLineIdentifiesTag(line, "ELASTIC");
        }

        /// <summary>
        /// Checks if line identifies simulation material solid section.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesSimulationMaterialSolidSection(string line)
        {
            return CheckIfLineIdentifiesTag(line, "SOLID SECTION");
        }

        /// <summary>
        /// Checks if line identifies simulation static.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesSimulationStatic(string line)
        {
            return CheckIfLineIdentifiesTag(line, "STATIC");
        }

        /// <summary>
        /// Checks if line identifies simulation step c load.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesSimulationStepCLoad(string line)
        {
            return CheckIfLineIdentifiesTag(line, "CLOAD");
        }

        /// <summary>
        /// Checks if line identifies tag.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="tag">The tag.</param>
        /// <returns></returns>
        private bool CheckIfLineIdentifiesTag(string line, string tag)
        {
            var parts = line.Split(',');

            var identifiesTag = false;
            if (parts.Length > 0)
            {
                identifiesTag = parts[0].Matches($"*{tag}");
            }

            return identifiesTag;
        }

        /// <summary>
        /// Clears the inp file.
        /// </summary>
        private void ClearInpFile()
        {
            File.WriteAllText(InpFile, string.Empty);
        }

        /// <summary>
        /// Firsts the part is numerical.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool FirstPartIsNumerical(string line)
        {
            string[] parts = line.Split(new char[] { ',' });

            bool isNumerical = false;
            int result;
            if (parts.Length > 0)
                isNumerical = int.TryParse(parts[0], out result);

            return isNumerical;
        }

        private string GetInpExponentialString(double value)
        {
            if (value >= 1)
            {
                return GetInpFloatString(value);
            }
            else
            {
                return $"{value:0.##E+00}";
            }
        }

        private string GetInpFloatString(double value)
        {
            string inpFloatString = string.Format(CultureInfo.InvariantCulture, "{0:0.##########}", value);
            // Add dot if necessary
            if (Math.Abs(value % 1) < 0.0000000001)
            {
                inpFloatString += CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
            }

            return inpFloatString;
        }

        /// <summary>
        /// Parses as double.
        /// </summary>
        /// <param name="doubleString">The double string.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        private bool ParseAsDouble(string doubleString, out double result)
        {
            result = 0;
            bool success = double.TryParse(doubleString, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
            return success;
        }

        /// <summary>
        /// Reads the tet mesh elements.
        /// </summary>
        private void ReadElements(string[] inpLines)
        {
            bool isTetMeshElSet = false;
            foreach (string line in inpLines)
            {
                if (FirstPartIsNumerical(line) && isTetMeshElSet)
                {
                    Part.Elements.Add(ReadLineAsPartElement(line));
                }
                else if (CheckIfLineIdentifiesElements(line))
                {
                    isTetMeshElSet = true;
                    ReadLineAsPartElementsHeader(line);
                }
                else if (Part.Elements.Count > 0)
                {
                    isTetMeshElSet = false;
                }
            }
        }

        /// <summary>
        /// Reads the header.
        /// </summary>
        private void ReadHeader(string[] inpLines)
        {
            bool readLineAsHeader = false;
            foreach (string line in inpLines)
            {
                if (CheckIfLineIdentifiesHeader(line))
                {
                    readLineAsHeader = true;
                }
                else if (line.Length < 2 || line.Substring(0, 2) != "**")
                {
                    readLineAsHeader = false;
                }
                else if (readLineAsHeader)
                {
                    HeaderLines.Add(line.Substring(3, line.Length - 3));
                }
            }
        }

        /// <summary>
        /// Reads the line as simulation n set.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not read index in simulation NSet</exception>
        private int[] ReadLineAsIntegerArray(string line)
        {
            string[] parts = line.Split(new char[] { ',' });

            int[] nodesIndices = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                bool parsedIndex = int.TryParse(parts[i], out nodesIndices[i]);
                if (!parsedIndex)
                {
                    throw new Exception("Could not read index in simulation NSet");
                }
            }

            return nodesIndices;
        }

        /// <summary>
        /// Reads the line as tet mesh coordinate.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not read coordinate in NSet</exception>
        private double[] ReadLineAsPartCoordinate(string line)
        {
            string[] parts = line.Split(new char[] { ',' });

            double[] coordinates = new double[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                bool parsedCoordinate = ParseAsDouble(parts[i], out coordinates[i - 1]);
                if (!parsedCoordinate)
                {
                    throw new Exception("Could not read coordinate in NSet");
                }
            }

            return coordinates;
        }

        /// <summary>
        /// Reads the line as tet mesh element.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Could not read index in elset</exception>
        private int[] ReadLineAsPartElement(string line)
        {
            string[] parts = line.Split(new char[] { ',' });

            int[] nodesIndices = new int[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                bool parsedCoordinate = int.TryParse(parts[i], out nodesIndices[i - 1]);
                if (!parsedCoordinate)
                {
                    throw new Exception("Could not read index in elset");
                }
            }

            return nodesIndices;
        }

        private void ReadLineAsPartElementsHeader(string line)
        {
            string[] parts = line.Split(new char[] { ',' });

            for (int i = 1; i < parts.Length; i++)
            {
                string[] property = parts[i].Split(new char[] { '=' });
                if (property.Length == 2)
                {
                    switch (property[0].ToLower().Trim())
                    {
                        case ("type"):
                            Part.ElementType = property[1].Trim();
                            break;

                        case ("elset"):
                            Part.ElementSetName = property[1].Trim();
                            break;
                    }
                }
            }
        }

        private string ReadLineAsPartHeader(string line)
        {
            string name = string.Empty;

            string[] parts = line.Split(new char[] { ',' });

            for (int i = 1; i < parts.Length; i++)
            {
                string[] property = parts[i].Split(new char[] { '=' });
                if (property.Length == 2 && property[0].ToLower() == "name")
                    name = property[1];
            }

            return name;
        }

        /// <summary>
        /// Reads the line as simulation boundary condition.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="boundaryNSet">The boundary n set.</param>
        /// <param name="boundaryFromAxis">The boundary from axis.</param>
        /// <param name="boundaryToAxis">The boundary to axis.</param>
        /// <param name="boundaryDisplacement">The boundary displacement.</param>
        private InpBoundaryCondition ReadLineAsSimulationBoundaryCondition(string line)
        {
            // Initialize
            string boundaryNSet = string.Empty;
            int boundaryFromAxis = 0;
            int boundaryToAxis = 0;
            double boundaryDisplacement = 0;

            string[] parts = line.Split(new char[] { ',' });

            if (parts.Length > 0)
            {
                boundaryNSet = parts[0];
            }
            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out boundaryFromAxis);
            }
            if (parts.Length > 2)
            {
                int.TryParse(parts[2], out boundaryToAxis);
            }
            if (parts.Length > 3)
            {
                ParseAsDouble(parts[3], out boundaryDisplacement);
            }

            return new InpBoundaryCondition(boundaryNSet, boundaryFromAxis, boundaryToAxis);
        }

        /// <summary>
        /// Reads the line as simulation material elasticity.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="eModulus">The e modulus.</param>
        /// <param name="poissonRatio">The poisson ratio.</param>
        private void ReadLineAsSimulationMaterialElasticity(string line, out double eModulus, out double poissonRatio)
        {
            // Initialize
            eModulus = 0;
            poissonRatio = 0;

            string[] parts = line.Split(new char[] { ',' });

            if (parts.Length > 0)
            {
                ParseAsDouble(parts[0], out eModulus);
            }
            if (parts.Length > 1)
            {
                ParseAsDouble(parts[1], out poissonRatio);
            }
        }

        /// <summary>
        /// Reads the line as simulation material.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private string ReadLineAsSimulationMaterialName(string line)
        {
            string[] parts = line.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string materialName = string.Empty;

            for (int i = 1; i < parts.Length; i++)
            {
                string[] materialProperty = parts[i].Split(new char[] { '=' });
                if (materialProperty.Length > 0)
                {
                    if (materialProperty[0].Matches("NAME"))
                    {
                        materialName = materialProperty[1];
                    }
                }
            }

            return materialName;
        }

        /// <summary>
        /// Reads the line as simulation step c load.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="cLoadNSetName">Name of the c load n set.</param>
        /// <param name="cLoadAxis">The c load axis.</param>
        /// <param name="cLoadForceValue">The c load force value.</param>
        private InpLoad ReadLineAsSimulationStepCLoad(string line)
        {
            // Initialize
            string cLoadNSetName = string.Empty;
            int cLoadAxis = 0;
            double cLoadForceValue = 0;

            string[] parts = line.Split(',');

            if (parts.Length > 0)
            {
                cLoadNSetName = parts[0];
            }
            if (parts.Length > 1)
            {
                int.TryParse(parts[1], out cLoadAxis);
            }
            if (parts.Length > 2)
            {
                ParseAsDouble(parts[2], out cLoadForceValue);
            }

            return new InpLoad(cLoadNSetName, cLoadAxis, cLoadForceValue);
        }
        /// <summary>
        /// Reads the tet mesh n set.
        /// </summary>
        private void ReadNodes(string[] inpLines)
        {
            bool isTetMeshNSet = false;
            foreach (string line in inpLines)
            {
                if (FirstPartIsNumerical(line) && isTetMeshNSet)
                {
                    Part.Nodes.Add(ReadLineAsPartCoordinate(line));
                }
                else if (CheckIfLineIdentifiesNodes(line))
                {
                    isTetMeshNSet = true;
                }
                else if (Part.Nodes.Count > 0)
                {
                    isTetMeshNSet = false;
                }
            }
        }

        /// <summary>
        /// Reads the simulation boundary conditions.
        /// </summary>
        private void ReadSimulationBoundaryConditions(string[] inpLines)
        {
            bool readLineAsSimulationBoundaryCondition = false;
            foreach (string line in inpLines)
            {
                if (readLineAsSimulationBoundaryCondition)
                {
                    InpBoundaryCondition boundaryCondition = ReadLineAsSimulationBoundaryCondition(line);
                    Simulation.BoundaryConditions.Add(boundaryCondition);

                    readLineAsSimulationBoundaryCondition = false;
                }
                else if (CheckIfLineIdentifiesSimulationBoundaryCondition(line))
                {
                    readLineAsSimulationBoundaryCondition = true;
                }
            }
        }

        /// <summary>
        /// Reads the simulation step.
        /// </summary>
        private void ReadSimulationLoads(string[] inpLines)
        {
            bool readLineAsSimulationStepCLoad = false;
            foreach (string line in inpLines)
            {
                if (readLineAsSimulationStepCLoad)
                {
                    InpLoad load = ReadLineAsSimulationStepCLoad(line);
                    Simulation.Loads.Add(load);

                    readLineAsSimulationStepCLoad = false;
                }
                else if (CheckIfLineIdentifiesSimulationStepCLoad(line))
                {
                    readLineAsSimulationStepCLoad = true;
                }
            }
        }

        /// <summary>
        /// Reads the simulation material.
        /// </summary>
        private void ReadSimulationMaterial(string[] inpLines)
        {
            bool readLineAsSimulationMaterialElasticity = false;
            foreach (string line in inpLines)
            {
                if (readLineAsSimulationMaterialElasticity)
                {
                    double elasticityModulus = 0;
                    double poissonRatio = 0;
                    ReadLineAsSimulationMaterialElasticity(line, out elasticityModulus, out poissonRatio);

                    Simulation.Material.ElasticityEModulus = elasticityModulus;
                    Simulation.Material.ElasticityPoissonRatio = poissonRatio;

                    readLineAsSimulationMaterialElasticity = false;
                }
                else if (CheckIfLineIdentifiesSimulationMaterial(line))
                {
                    Simulation.Material.Name = ReadLineAsSimulationMaterialName(line);
                }
                else if (CheckIfLineIdentifiesSimulationMaterialElasticity(line))
                {
                    // Read next line as elasticity
                    readLineAsSimulationMaterialElasticity = true;
                }
            }
        }

        /// <summary>
        /// Reads the simulation n sets.
        /// </summary>
        private void ReadSimulationNSets(string[] inpLines)
        {
            Dictionary<string, List<int>> nSets = ReadSimulationSets(inpLines, "NSET");
            foreach (KeyValuePair<string, List<int>> nSet in nSets)
            {
                if (nSet.Key.ToLower().Contains("_load_"))
                {
                    Simulation.NSetsLoad.Add(nSet.Key, nSet.Value);
                }
                else if (nSet.Key.ToLower().Contains("_bc_"))
                {
                    Simulation.NSetsBoundaryConditions.Add(nSet.Key, nSet.Value);
                }
                else
                {
                    throw new Exception(string.Format("Unrecognized NSet name {0}. Could not determine whether it defines a load or boundary condition NSet", nSet.Key));
                }
            }
        }

        /// <summary>
        /// Reads the simulation sets.
        /// </summary>
        /// <param name="tag">The tag.</param>
        /// <returns></returns>
        private Dictionary<string, List<int>> ReadSimulationSets(string[] inpLines, string tag)
        {
            Dictionary<string, List<int>> sets = new Dictionary<string, List<int>>();
            bool generator = false;

            bool isTheSet = false;
            string name = string.Empty;
            List<int> indices = new List<int>();
            foreach (string line in inpLines)
            {
                if (FirstPartIsNumerical(line) && isTheSet)
                {
                    if (generator)
                    {
                        int[] generatorDefinition = ReadLineAsIntegerArray(line);

                        if (generatorDefinition.Length == 3)
                        {
                            indices.AddRange(MathUtilities.Range(generatorDefinition[0], generatorDefinition[1], generatorDefinition[2]));
                            sets.Add(name, indices);
                            return sets;
                        }
                        else
                            throw new Exception("Unknown generator definition");
                    }
                    else
                    {
                        indices.AddRange(ReadLineAsIntegerArray(line).ToList());
                    }
                }
                else if (CheckIfLineIdentifiesTag(line, tag))
                {
                    // Add to simulation NSets
                    if (indices.Count > 0)
                    {
                        sets.Add(name, indices);
                    }

                    // Get name
                    string[] parts = line.Split(new char[] { ',' });
                    foreach (string part in parts)
                    {
                        string[] property = part.Split(new char[] { '=' });
                        if (property.Length == 2)
                        {
                            if (property[0].Matches(tag))
                            {
                                name = property[1];
                            }
                        }
                    }
                    // Reset
                    indices = new List<int>();

                    generator = line.ToLower().Contains("generate");
                    isTheSet = true;
                }
                else
                {
                    // Add to simulation NSets (for last iteration only)
                    if (indices.Count > 0 && !sets.ContainsKey(name))
                    {
                        sets.Add(name, indices);
                    }

                    isTheSet = false;
                }
            }

            return sets;
        }
        /// <summary>
        /// Writes the tet mesh el set.
        /// </summary>
        private void WriteElements(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendCultureInvariantLine("*ELEMENT, TYPE=C3D4, ELSET=ALL_ELEMENTS");
            for (int i = 0; i < Part.Elements.Count; i++)
            {
                string nodeText = string.Format("{0:D}", i + 1);
                foreach (int nodeIndex in Part.Elements[i])
                {
                    nodeText += string.Format(", {0:D}", nodeIndex);
                }

                stringBuilder.AppendCultureInvariantLine(nodeText);
            }
        }

        /// <summary>
        /// Writes the header.
        /// </summary>
        private void WriteHeader(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendCultureInvariantLine("*Heading");
            foreach (string line in HeaderLines)
            {
                stringBuilder.AppendCultureInvariantLine(string.Format("** {0}", line.Trim()));
            }
        }

        /// <summary>
        /// Writes the indices.
        /// </summary>
        /// <param name="indices">The indices.</param>
        /// <param name="indicesPerLine">The indices per line.</param>
        private void WriteIndices(ref StringBuilder stringBuilder, List<int> indices, object indicesPerLine)
        {
            string nodeText = string.Empty;
            for (int i = 0; i < indices.Count; i++)
            {
                nodeText += string.Format("{0:D}, ", indices[i]);
                if (i > 0 && (i + 1) % 16 == 0)
                {
                    stringBuilder.AppendCultureInvariantLine(nodeText.Substring(0, nodeText.Length - 2));
                    nodeText = string.Empty;
                }
            }
            if (nodeText != string.Empty)
            {
                stringBuilder.AppendCultureInvariantLine(nodeText.Substring(0, nodeText.Length - 2));
            }
        }
        /// <summary>
        /// Writes the tet mesh n set.
        /// </summary>
        private void WriteNodes(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendCultureInvariantLine("*NODE");
            for (int i = 0; i < Part.Nodes.Count; i++)
            {
                string nodeText = string.Format("{0:D}", i + 1);
                foreach (double coordinate in Part.Nodes[i])
                {
                    nodeText += string.Format(", {0}", GetInpFloatString(coordinate));
                }

                stringBuilder.AppendCultureInvariantLine(nodeText);
            }
        }

        private void WriteNSets(ref StringBuilder stringBuilder, Dictionary<string, List<int>> nSets)
        {
            foreach (KeyValuePair<string, List<int>> nSet in nSets)
            {
                WriteSingleLine(ref stringBuilder, "*NSET, NSET={0}", nSet.Key.Trim());
                WriteIndices(ref stringBuilder, nSet.Value, IndicesPerLine);
            }
        }

        /// <summary>
        /// Writes the simulation boundary conditions.
        /// </summary>
        private void WriteSimulationBoundaryConditions(ref StringBuilder stringBuilder)
        {
            int i = 0;
            foreach (InpBoundaryCondition boundaryCondition in Simulation.BoundaryConditions)
            {
                i++;

                stringBuilder.AppendCultureInvariantLine("*BOUNDARY");
                stringBuilder.AppendCultureInvariantLine("{0}, {1:D}, {2:D}", boundaryCondition.BoundaryNSetName, boundaryCondition.BoundaryFromAxis, boundaryCondition.BoundaryToAxis);
            }
        }

        /// <summary>
        /// Writes the simulation loads.
        /// </summary>
        private void WriteSimulationLoads(ref StringBuilder stringBuilder)
        {
            int i = 0;
            foreach (InpLoad load in Simulation.Loads)
            {
                i++;

                stringBuilder.AppendCultureInvariantLine("*CLOAD");
                stringBuilder.AppendCultureInvariantLine("{0}, {1:D}, {2:0.##########}", load.NSetName, load.Axis, load.ForceValue);
            }
        }

        /// <summary>
        /// Writes the simulation material.
        /// </summary>
        private void WriteSimulationMaterial(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendCultureInvariantLine("*MATERIAL, NAME={0}", Simulation.Material.Name.Trim());
            stringBuilder.AppendCultureInvariantLine("*ELASTIC");
            stringBuilder.AppendCultureInvariantLine("{0}, {1}", GetInpFloatString(Simulation.Material.ElasticityEModulus), GetInpFloatString(Simulation.Material.ElasticityPoissonRatio));
        }
        private void WriteSimulationOutputs(ref StringBuilder stringBuilder)
        {
            stringBuilder.AppendCultureInvariantLine("*NODE FILE");
            stringBuilder.AppendCultureInvariantLine("U,E");
            stringBuilder.AppendCultureInvariantLine("* EL FILE");
            stringBuilder.AppendCultureInvariantLine("S");
        }

        /// <summary>
        /// Writes the simulation solid section.
        /// </summary>
        private void WriteSimulationSolidSection(ref StringBuilder stringBuilder)
        {
            WriteSingleLine(ref stringBuilder, "*Solid Section, material={0}, elset=ALL_ELEMENTS", Simulation.Material.Name.Trim());
        }

        /// <summary>
        /// Writes the simulation static.
        /// </summary>
        private void WriteSimulationStatic(ref StringBuilder stringBuilder)
        {
            WriteSingleLine(ref stringBuilder, "*Static");
        }

        /// <summary>
        /// Writes the step end.
        /// </summary>
        private void WriteSimulationStepEnd(ref StringBuilder stringBuilder)
        {
            WriteSingleLine(ref stringBuilder, "*End Step");
        }

        /// <summary>
        /// Writes the step start.
        /// </summary>
        private void WriteSimulationStepStart(ref StringBuilder stringBuilder)
        {
            WriteSingleLine(ref stringBuilder, "*Step");
        }
        /// <summary>
        /// Writes the single line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="arg">The argument.</param>
        private void WriteSingleLine(ref StringBuilder stringBuilder, string line, params object[] arg)
        {
            stringBuilder.AppendCultureInvariantLine(line, arg);
        }
    }
}