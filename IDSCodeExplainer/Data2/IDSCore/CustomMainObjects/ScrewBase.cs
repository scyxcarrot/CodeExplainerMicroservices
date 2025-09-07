using IDS.Core.ImplantDirector;
using IDS.Core.PluginHelper;
using IDS.Core.Utilities;
using Rhino;
using Rhino.Collections;
using Rhino.DocObjects;
using Rhino.DocObjects.Custom;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IDS.Core.ImplantBuildingBlocks
{
    public abstract class ScrewBase<TImplantDirector, TScrewType, TScrewAidesType> : 
        CustomBrepObject , IComparable<ScrewBase<TImplantDirector, TScrewType, TScrewAidesType>>
        where TImplantDirector : class, IImplantDirector 
    {
        protected ScrewBase() 
        { }

        protected ScrewBase(Brep brep) : base(brep)
        { }

        protected ScrewBase(RhinoObject other, bool copyAttributes) : this(other.Geometry as Brep)
        {
            // Replace the object in the document or create new one
            if (copyAttributes)
            {
                Attributes = other.Attributes;
            }
        }

        /// <summary>
        /// De-serialize member variables from archive.
        /// </summary>
        /// <param name="userDict">The user dictionary.</param>
        public virtual void DeArchive(ArchivableDictionary userDict)
        {
            // Load position parameters
            HeadPoint = userDict.GetPoint3d(KeyHeadPoint, Point3d.Unset);
            TipPoint = userDict.GetPoint3d(KeyTipPoint, Point3d.Unset);
            Index = userDict.GetInteger(KeyIndex, 0);
            _fixedLength = userDict.GetDouble(KeyFixedLength, 0.0);
        }

        public abstract string GenerateNameForMimics();

        /// <summary>
        /// The maximum body length
        /// </summary>
        public abstract double MaximumBodyLength { get; }

        /// <summary>
        /// The key axial offset
        /// </summary>
        protected const string KeyAxialOffset = "axial_offset";

        /// <summary>
        /// The key fixed length
        /// </summary>
        protected const string KeyFixedLength = "fixed_length";

        /// <summary>
        /// The key head point
        /// </summary>
        protected const string KeyHeadPoint = "head_point";

        /// <summary>
        /// The key index
        /// </summary>
        protected const string KeyIndex = "index";

        /// <summary>
        /// The key screw type
        /// </summary>
        protected const string KeyScrewType = "screw_type";

        /// <summary>
        /// The key tip point
        /// </summary>
        protected const string KeyTipPoint = "tip_point";

        /// <summary>
        /// The fixed length
        /// </summary>
        protected double _fixedLength;

        /// <summary>
        /// Gets or sets the length of the fixed.
        /// </summary>
        /// <value>
        /// The length of the fixed.
        /// </value>
        public double FixedLength
        {
            get
            {
                return _fixedLength;
            }
            set
            {
                _fixedLength = value; // length is now fixed
                // Recalculate tip point
                if (Math.Abs(value) > 0.0001)
                {
                    TipPoint = HeadPoint + Direction * value;
                }
            }
        }

        /// <summary>
        /// Gets the body origin.
        /// </summary>
        /// <value>
        /// The body origin.
        /// </value>
        public abstract Point3d BodyOrigin
        {
            get;
        }

        /// <summary>
        /// Gets the center line.
        /// </summary>
        /// <value>
        /// The center line.
        /// </value>
        public Line CenterLine => new Line(TipPoint - Radius * Direction, BodyOrigin);

        /// <summary>
        /// Gets or sets the type of the screw.
        /// </summary>
        /// <value>
        /// The type of the screw.
        /// </value>
        public TScrewType ScrewType
        {
            get;
            set;
        }

        /// <summary>
        /// The screw aides
        /// </summary>
        public Dictionary<TScrewAidesType, Guid> ScrewAides { get; set; } = new Dictionary<TScrewAidesType, Guid>();

        /// <summary>
        /// Gets the diameter.
        /// </summary>
        /// <value>
        /// The diameter.
        /// </value>
        public double Diameter => GetDiameter();

        /// <summary>
        /// Gets the diameter.
        /// </summary>
        /// <param name="screwType">Type of the screw.</param>
        /// <returns></returns>
        protected abstract double GetDiameter();

        /// <summary>
        /// Gets the screw vector.
        /// </summary>
        /// <value>
        /// The screw vector.
        /// </value>
        protected Vector3d ScrewVector => TipPoint - HeadPoint;

        /// <summary>
        /// Gets the direction.
        /// </summary>
        /// <value>
        /// The direction.
        /// </value>
        public Vector3d Direction
        {
            get
            {
                var screwVector = ScrewVector;
                screwVector.Unitize();
                return screwVector;
            }
        }

        /// <summary>
        /// Gets or sets the director.
        /// </summary>
        /// <value>
        /// The director.
        /// </value>
        public TImplantDirector Director
        {
            get;
            set;
        }

        /// <summary>
        /// Distances the in bone.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        public abstract double GetDistanceInBone();

        protected double CalculateDistanceInBone(Mesh target)
        {
            // Shoot rays along screw direction to find intersection with bone
            var rayorigin = new List<Point3d>
            {
                BodyOrigin
            };
            List<Point3d[]> rayHits;
            List<double[]> hitDists;
            List<int[]> hitFaceIdx;
            target.IntersectWithRays(rayorigin, Direction, MaximumBodyLength, 20.0, out rayHits, out hitDists, out hitFaceIdx);
            var bonedist = 0.0;

            if (hitDists.Count <= 0 || hitDists[0].Length <= 0)
            {
                return bonedist;
            }

            Array.Sort(hitDists[0]);
            // Cycle through all the hit points and sum bone penetration distances Depends on
            // whether first point is inside or outside, so use
            var startInside = false;
            var posDists = new List<double>();
            for (var i = 0; i < hitDists[0].Length; i++)
            {
                if (hitDists[0][i] < 0)
                {
                    startInside = true;
                }
                else if (hitDists[0][i] >= 0 && hitDists[0][i] <= BodyLength)
                {
                    posDists.Add(hitDists[0][i]);
                }
                else if (hitDists[0][i] >= 0 && hitDists[0][i] > BodyLength && posDists.Count != 0 &&
                         Math.Abs(posDists.Last() - BodyLength) > 0.0001)
                {
                    posDists.Add(BodyLength);
                    break;
                }
                else
                {
                    return 0.0; // something went wrong
                }
            }
            if (startInside)
            {
                posDists.Insert(0, 0.0);
            }

            bonedist = (posDists[posDists.Count - 1] - posDists[0]);
            return bonedist;
        }

        /// <summary>
        /// Gets the head point.
        /// </summary>
        /// <value>
        /// The head point.
        /// </value>
        public abstract Point3d HeadPoint
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets or sets the index.
        /// </summary>
        /// <value>
        /// The index.
        /// </value>
        public int Index
        {
            get;
            set;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is bicortical.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is bicortical; otherwise, <c>false</c>.
        /// </value>
        public abstract bool IsBicortical { get; }

        /// <summary>
        /// Gets the radius.
        /// </summary>
        /// <value>
        /// The radius.
        /// </value>
        public double Radius => Diameter / 2.0;

        /// <summary>
        /// Gets the screw line.
        /// </summary>
        /// <value>
        /// The screw line.
        /// </value>
        public Line ScrewLine => new Line(TipPoint, HeadPoint);

        /// <summary>
        /// Gets the tip point.
        /// </summary>
        /// <value>
        /// The tip point.
        /// </value>
        public abstract Point3d TipPoint
        {
            get;
            protected set;
        }

        /// <summary>
        /// Gets the total length.
        /// </summary>
        /// <value>
        /// The total length.
        /// </value>
        public double TotalLength => (TipPoint - HeadPoint).Length;

        /// <summary>
        /// Gets the alignment transform.
        /// </summary>
        /// <value>
        /// The alignment transform.
        /// </value>
        public Transform AlignmentTransform
        {
            get
            {
                var rotation = Transform.Rotation(-Plane.WorldXY.ZAxis, Direction, Plane.WorldXY.Origin);
                var translation = Transform.Translation(HeadPoint - Plane.WorldXY.Origin);
                return Transform.Multiply(translation, rotation);
            }
        }

        /// <summary>
        /// Gets the length of the body.
        /// </summary>
        /// <value>
        /// The length of the body.
        /// </value>
        public double BodyLength => (TipPoint - BodyOrigin).Length;

        /// <summary>
        /// Gets the body line.
        /// </summary>
        /// <value>
        /// The body line.
        /// </value>
        protected Line BodyLine => new Line(TipPoint, BodyOrigin);

        /// <summary>
        /// Gets the screw lengths.
        /// </summary>
        /// <value>
        /// The screw lengths.
        /// </value>
        protected abstract double[] ScrewLengths { get; }

        /// <summary>
        /// Distances the until bone.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <returns></returns>
        public abstract double GetDistanceUntilBone();

        protected double CalculateDistanceUntilBone(Mesh target)
        {
            // Shoot rays along screw direction to find intersection with bone
            var rayorigin = new List<Point3d>();
            rayorigin.Add(BodyOrigin);
            List<Point3d[]> rayHits;
            List<double[]> hitDists;
            List<int[]> hitFaceIdx;
            target.IntersectWithRays(rayorigin, Direction, MaximumBodyLength, 20.0, out rayHits, out hitDists, out hitFaceIdx);
            var bonedist = 0.0;

            if (hitDists.Count <= 0 || hitDists[0].Length <= 0)
            {
                return bonedist;
            }

            Array.Sort(hitDists[0]);
            // Cycle through all the hit points and sum bone penetration distances Depends on
            // whether first point is inside or outside, so use
            var posDists = new List<double>();
            for (var i = 0; i < hitDists[0].Length; i++)
            {
                if (hitDists[0][i] < 0)
                {
                    return 0;
                }

                if (hitDists[0][i] >= 0 && hitDists[0][i] <= BodyLength)
                {
                    posDists.Add(hitDists[0][i]);
                    break;
                }

                if (!(hitDists[0][i] >= 0) || !(hitDists[0][i] > BodyLength) || posDists.Count == 0 ||
                    !(Math.Abs(posDists.Last() - BodyLength) > 0.0001))
                {
                    return double.MaxValue; // something went wrong
                }

                posDists.Add(BodyLength);
                break;
            }
            bonedist = (posDists[0]);
            return bonedist;
        }

        /// <summary>
        /// Compares the current object with another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// A value that indicates the relative order of the objects being compared. The return value has the following meanings: Value Meaning Less than zero This object is less than the <paramref name="other" /> parameter.Zero This object is equal to <paramref name="other" />. Greater than zero This object is greater than <paramref name="other" />.
        /// </returns>
        public int CompareTo(ScrewBase<TImplantDirector, TScrewType, TScrewAidesType> other)
        {
            return Index - other.Index;
        }

        /// <summary>
        /// Deletes this instance.
        /// </summary>
        /// <returns></returns>
        public bool Delete()
        {
            var objectManager = new ObjectManager(Director);

            //Dependencies.DeleteScrewDependencies(this);
            objectManager.DeleteObject(this.Id);

            return true;
        }

        /// <summary>
        /// Gets the length of the available.
        /// </summary>
        /// <returns></returns>
        public abstract double GetAvailableLength();

        /// <summary>
        /// Serialize member variables to user dictionary.
        /// </summary>
        public virtual void PrepareForArchiving()
        {
            var userDict = Attributes.UserDictionary;
            userDict.Set(KeyHeadPoint, HeadPoint);
            userDict.Set(KeyTipPoint, TipPoint);
            userDict.Set(KeyIndex, Index);
            userDict.Set(KeyFixedLength, FixedLength);
            CommitChanges();
        }

        /// <summary>
        /// Sets the specified old screw identifier.
        /// </summary>
        /// <param name="oldScrewId">The old screw identifier.</param>
        /// <param name="recalibrate">if set to <c>true</c> [recalibrate].</param>
        /// <param name="update">if set to <c>true</c> [update].</param>
        /// <returns></returns>
        public abstract void Set(Guid oldScrewId, bool recalibrate, bool update);

        /// <summary>
        /// Sets the length of the available.
        /// </summary>
        /// <returns></returns>
        public bool SetAvailableLength()
        {
            // Get the available length
            var nearestLength = GetAvailableLength();

            // Set the new tip point
            TipPoint = HeadPoint + Direction * nearestLength;

            // success
            return true;
        }

        /// <summary>
        /// Updates this instance.
        /// </summary>
        /// <returns></returns>
        public bool Update()
        {
            return Update(Id);
        }

        /// <summary>
        /// Updates the specified old identifier.
        /// </summary>
        /// <param name="oldId">The old identifier.</param>
        /// <returns></returns>
        protected abstract bool Update(Guid oldId);

        /// <summary>
        /// This call informs an object it is about to be added to the list of
        /// active objects in the document.
        /// </summary>
        /// <param name="doc"></param>
        protected override void OnAddToDocument(RhinoDoc doc)
        {
            base.OnAddToDocument(doc);
            
            if (Director == null)
            {
                return;
            }
            // Disable undo recording so that Ctrl-Z does not restore the screw aides Screw aide
            // creation is controlled by OnAddToDocument (which is also triggered when Ctrl-Z is pressed)
            Director.Document.UndoRecordingEnabled = false;

            CreateAides();

            // Restart recording actions for Ctrl-Z
            Director.Document.UndoRecordingEnabled = true;
        }

        /// <summary>
        /// This call informs an object it is about to be deleted.
        /// Some objects, like clipping planes, need to do a little extra cleanup
        /// before they are deleted.
        /// </summary>
        /// <param name="doc"></param>
        protected override void OnDeleteFromDocument(RhinoDoc doc)
        {
            base.OnDeleteFromDocument(doc);

            // Disable undo recording so that Ctrl-Z does not restore the screw aides Screw aide
            // creation is controlled by OnAddToDocument (which is also triggered when Ctrl-Z is pressed)
            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = false;
            }

            var objectManager = new ObjectManager(Director);

            // Remove all screw aides from the document
            foreach (var id in ScrewAides.Values)
            {
                objectManager.DeleteObject(id);
            }

            // Empty the screw aide dictionary of the screw
            ScrewAides.Clear();

            if (Director != null)
            {
                Director.Document.UndoRecordingEnabled = true;
            }
        }

        /// <summary>
        /// Called when this a new instance of this object is created and copied from
        /// an existing object
        /// </summary>
        /// <param name="source"></param>
        protected override void OnDuplicate(RhinoObject source)
        {
            base.OnDuplicate(source);

            var other = source as ScrewBase<TImplantDirector, TScrewType, TScrewAidesType>;

            if (other == null)
            {
                return;
            }

            Director = other.Director;
            Index = other.Index;
            HeadPoint = other.HeadPoint;
            TipPoint = other.TipPoint;
            _fixedLength = other._fixedLength;
            ScrewType = other.ScrewType;
        }

        /// <summary>
        /// Creates the aides.
        /// </summary>
        /// <returns></returns>
        protected abstract void CreateAides();

        /// <summary>
        /// Corticals the bites.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <param name="bodyOrigin"></param>
        /// <param name="bodyLength"></param>
        /// <returns></returns>
        public int GetCorticalBites(Mesh target, Point3d bodyOrigin, double bodyLength)
        {
            // Shoot rays along screw direction to find intersection with bone
            var rayorigin = new List<Point3d>
            {
                bodyOrigin
            };
            List<Point3d[]> rayHits;
            List<double[]> hitDists;
            List<int[]> hitFaceIdx;
            target.IntersectWithRays(rayorigin, Direction, MaximumBodyLength, 10.0, out rayHits, out hitDists, out hitFaceIdx);
            // Determine penetration, depending on first two intersection
            var bites = 0;

            if (hitDists.Count <= 0 || hitDists[0].Length <= 0)
            {
                return bites;
            }

            Array.Sort(hitDists[0]);
            // Cycle through all the hit points and sum bone penetration distances Depends on
            // whether first point is inside or outside, so use
            for (var i = 0; i < hitDists[0].Length; i++)
            {
                if (hitDists[0][i] >= 0 && hitDists[0][i] <= bodyLength)
                {
                    bites++;
                }
            }

            return bites;
        }

        /// <summary>
        /// Fixations the specified target.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="screwDatabase">The screw database.</param>
        /// <param name="bodyOrigin"></param>
        /// <param name="bodyLength"></param>
        /// <returns></returns>
        protected string GetFixation(Mesh target, Point3d bodyOrigin, double bodyLength)
        {
            var bites = GetCorticalBites(target, bodyOrigin, bodyLength);

            if (bites == 1)
            {
                return "UNI";
            }

            return bites > 1 ? "BI" : "?";
        }

        /// <summary>
        /// Gets the letter.
        /// </summary>
        /// <value>
        /// The letter.
        /// </value>
        public string GetScrewLetter()
        {
            // First letter
            var indexD = (double)Index;
            var screwChar = Convert.ToChar((Index - 1) % 26 + 65);
            var screwChars = screwChar.ToString();

            // If the index is lower than 26, one character suffices; return the result.
            if (Index <= 26)
            {
                return screwChars;
            }

            // Add more characters for indices larger than 26, larger than 676,...
            var group = (int)Math.Floor((indexD - 1) / 26);
            var i = 2;
            while (group != 0)
            {
                screwChar = Convert.ToChar(group - 1 + 65);
                screwChars = screwChar + screwChars;
                group = (int)Math.Floor((indexD - 1) / (Math.Pow(26, i)));
                i += 1;
            }
            return screwChars;
        }

        /// <summary>
        /// Calculates the default length of the screw.
        /// </summary>
        /// <param name="screwType">Type of the screw.</param>
        /// <param name="targetMeshTip">The target mesh tip.</param>
        /// <param name="headPoint">The head point.</param>
        /// <param name="rayDir">The ray dir.</param>
        /// <param name="dist">The dist.</param>
        /// <param name="maxRayDist">The maximum ray dist.</param>
        /// <returns></returns>
        protected bool CalculateDefaultScrewLength(Mesh targetMeshTip, Point3d headPoint, Vector3d rayDir, out double dist, double maxRayDist = 200.0)
        {
            // init
            dist = 0;
            int[] faceIds;

            // Make sure facenormals are there
            targetMeshTip.FaceNormals.ComputeFaceNormals();

            // Create ray
            rayDir.Unitize();
            var rayLine = new Line(headPoint, rayDir, maxRayDist);

            // Intersect mesh with line
            var hitPts = Intersection.MeshLine(targetMeshTip, rayLine, out faceIds);
            if (faceIds == null)
            {
                return false;
            }

            // Loop over all intersection points and select the last intersection point going out of
            // the bone
            var hitsAndFaceIds = hitPts.Zip(faceIds, (h, f) => new { HitPt = h, FaceId = f });
            foreach (var hf in hitsAndFaceIds)
            {
                var theNormal = targetMeshTip.FaceNormals[hf.FaceId];
                // if ray is going into the bone, do not consider the point
                if (theNormal * rayLine.Direction <= 0)
                {
                    continue;
                }

                // if ray is going out of the bone, check for max length
                var rayLength = (rayLine.From - hf.HitPt).Length;
                if (rayLength > dist)
                {
                    dist = rayLength;
                }
            }

            // Add the radius to the length to make sure the screw sticks out of the bone
            dist = dist + GetDiameter() / 2.0;

            // Success
            return true;
        }
    }
}
