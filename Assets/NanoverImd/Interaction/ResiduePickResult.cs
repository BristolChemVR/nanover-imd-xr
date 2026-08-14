using System.Collections.Generic;

namespace NanoverImd.Interaction
{
    /// <summary>
    /// Describes a successful, read-only residue pick.
    /// </summary>
    public sealed class ResiduePickResult
    {
        /// <summary>
        /// The nearest particle that caused the residue to be picked.
        /// </summary>
        public int ParticleIndex { get; }

        /// <summary>
        /// The index of the picked residue in the current frame.
        /// </summary>
        public int ResidueIndex { get; }

        /// <summary>
        /// All particle indices belonging to the picked residue.
        /// </summary>
        public IReadOnlyList<int> ParticleIndices { get; }

        public ResiduePickResult(int particleIndex,
                                 int residueIndex,
                                 IReadOnlyList<int> particleIndices)
        {
            ParticleIndex = particleIndex;
            ResidueIndex = residueIndex;
            ParticleIndices = new List<int>(particleIndices).AsReadOnly();
        }
    }
}
