using Rhino.Collections;

namespace IDS.Core.Fea
{
    public class Material
    {
        private const string KeyMaterialName = "material_name";
        private const string KeyMaterialElasticityEModulus = "material_emodulus";
        private const string KeyMaterialElasticityPoissonRatio = "material_poissonratio";
        private const string KeyMaterialUltimateTensileStrength = "material_uts";
        private const string KeyMaterialFatigueLimit = "material_fatiguelimit";

        /// <summary>
        /// Initializes a new instance of the <see cref="Material"/> class.
        /// </summary>
        public Material() : this(string.Empty, 0, 0, 0, 0)
        {
            // empty
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Material"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="elasticityEModulus">The elasticity e modulus.</param>
        /// <param name="elasticityPoissonRatio">The elasticity poisson ratio.</param>
        /// <param name="ultimateTensileStrength">The ultimate tensile strength.</param>
        /// <param name="fatigueLimit">The fatigue limit.</param>
        public Material(string name, double elasticityEModulus, double elasticityPoissonRatio, double ultimateTensileStrength, double fatigueLimit)
        {
            this.Name = name;
            this.ElasticityEModulus = elasticityEModulus;
            this.ElasticityPoissonRatio = elasticityPoissonRatio;
            this.UltimateTensileStrength = ultimateTensileStrength;
            this.FatigueLimit = fatigueLimit;
        }

        /// <summary>
        /// Gets the archiveable dictionary material.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <param name="materialDictionary"></param>
        /// <returns></returns>
        public Material(ArchivableDictionary materialDictionary) : this(materialDictionary.GetString(KeyMaterialName),
                                                                           materialDictionary.GetDouble(KeyMaterialElasticityEModulus),
                                                                           materialDictionary.GetDouble(KeyMaterialElasticityPoissonRatio),
                                                                           materialDictionary.GetDouble(KeyMaterialUltimateTensileStrength),
                                                                           materialDictionary.GetDouble(KeyMaterialFatigueLimit))
        {

        }

        /// <summary>
        /// Gets or sets the elasticity e modulus.
        /// </summary>
        /// <value>
        /// The elasticity e modulus.
        /// </value>
        public double ElasticityEModulus { get; set; }

        /// <summary>
        /// Gets or sets the elasticity poisson ratio.
        /// </summary>
        /// <value>
        /// The elasticity poisson ratio.
        /// </value>
        public double ElasticityPoissonRatio { get; set; }

        /// <summary>
        /// Gets or sets the fatigue limit.
        /// </summary>
        /// <value>
        /// The fatigue limit.
        /// </value>
        public double FatigueLimit { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the ultimate tensile strength.
        /// </summary>
        /// <value>
        /// The ultimate tensile strength.
        /// </value>
        public double UltimateTensileStrength { get; set; }

        /// <summary>
        /// Gets the material archiveable dictionary.
        /// </summary>
        /// <param name="material">The material.</param>
        /// <returns></returns>
        public ArchivableDictionary ToArchiveableDictionary()
        {
            var materialArchiveableDictionary = new ArchivableDictionary();
            materialArchiveableDictionary.Set(KeyMaterialName, Name);
            materialArchiveableDictionary.Set(KeyMaterialElasticityEModulus, ElasticityEModulus);
            materialArchiveableDictionary.Set(KeyMaterialElasticityPoissonRatio, ElasticityPoissonRatio);
            materialArchiveableDictionary.Set(KeyMaterialUltimateTensileStrength, UltimateTensileStrength);
            materialArchiveableDictionary.Set(KeyMaterialFatigueLimit, FatigueLimit);

            return materialArchiveableDictionary;
        }
    }
}