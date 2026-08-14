using System;
using Nanover.Core.Science;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Immutable information about one residue in a protein sequence.
    /// Selection membership is stored separately from this topology information.
    /// </summary>
    public sealed class ProteinResidueInfo
    {
        /// <summary>
        /// The index of the residue in the current frame.
        /// </summary>
        public int ResidueIndex { get; }

        /// <summary>
        /// The entity (for example, a protein chain) containing the residue.
        /// A value of -1 indicates that no entity information is available.
        /// </summary>
        public int EntityIndex { get; }

        /// <summary>
        /// The residue name provided by the current frame, such as ALA or MSE.
        /// </summary>
        public string ResidueName { get; }

        /// <summary>
        /// The recognised standard amino acid, or null for a non-standard residue.
        /// </summary>
        public AminoAcid StandardAminoAcid { get; }

        /// <summary>
        /// Whether this residue is one of the recognised standard amino acids.
        /// </summary>
        public bool IsStandard => StandardAminoAcid != null;

        /// <summary>
        /// The single-letter sequence code, using X for a non-standard residue.
        /// </summary>
        public char SequenceCode => StandardAminoAcid?.SingleLetterCode ?? 'X';

        public ProteinResidueInfo(int residueIndex,
                                  int entityIndex,
                                  string residueName)
        {
            if (residueIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(residueIndex));
            if (entityIndex < -1)
                throw new ArgumentOutOfRangeException(nameof(entityIndex));
            if (string.IsNullOrWhiteSpace(residueName))
                throw new ArgumentException("A residue name is required.",
                                            nameof(residueName));

            ResidueIndex = residueIndex;
            EntityIndex = entityIndex;
            ResidueName = residueName.Trim();
            StandardAminoAcid = AminoAcid.GetAminoAcidFromResidue(ResidueName);
        }
    }
}
