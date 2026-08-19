using System;
using System.Collections.Generic;
using Nanover.Core.Science;
using Nanover.Frame;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Builds immutable protein-sequence topology from a simulation frame.
    /// </summary>
    public static class ProteinSequenceBuilder
    {
        private const string BackboneNitrogen = "N";
        private const string BackboneAlphaCarbon = "CA";
        private const string BackboneCarbonylCarbon = "C";

        /// <summary>
        /// Builds the protein sequence described by <paramref name="frame"/>.
        /// Standard amino acids are always included. A non-standard residue is
        /// included when it contains the N, CA, and C protein-backbone atoms.
        /// </summary>
        public static ProteinSequence Build(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            var residueNames = frame.ResidueNames;
            if (residueNames == null || residueNames.Length == 0)
                return new ProteinSequence(new ProteinResidueInfo[0]);

            var atomNamesByResidue = GetAtomNamesByResidue(
                frame.ParticleResidues,
                frame.ParticleNames,
                residueNames.Length);
            var particleIndicesByResidue = GetParticleIndicesByResidue(
                frame.ParticleResidues,
                residueNames.Length);
            var residues = new List<ProteinResidueInfo>();

            for (var residueIndex = 0;
                 residueIndex < residueNames.Length;
                 residueIndex++)
            {
                var residueName = residueNames[residueIndex];
                if (string.IsNullOrWhiteSpace(residueName))
                    continue;

                residueName = residueName.Trim();
                if (!IsProteinResidue(residueIndex,
                                      residueName,
                                      atomNamesByResidue))
                    continue;

                var entityIndex = GetEntityIndex(frame.ResidueEntities,
                                                 residueIndex);
                IReadOnlyList<int> particleIndices =
                    particleIndicesByResidue.TryGetValue(residueIndex,
                                                         out var indices)
                        ? indices
                        : Array.Empty<int>();
                residues.Add(new ProteinResidueInfo(residueIndex,
                                                    entityIndex,
                                                    residueName,
                                                    particleIndices));
            }

            return new ProteinSequence(residues);
        }

        private static Dictionary<int, HashSet<string>> GetAtomNamesByResidue(
            int[] particleResidues,
            string[] particleNames,
            int residueCount)
        {
            var atomNamesByResidue =
                new Dictionary<int, HashSet<string>>();

            if (particleResidues == null
                || particleNames == null
                || particleResidues.Length != particleNames.Length)
                return atomNamesByResidue;

            for (var particleIndex = 0;
                 particleIndex < particleResidues.Length;
                 particleIndex++)
            {
                var residueIndex = particleResidues[particleIndex];
                if (residueIndex < 0 || residueIndex >= residueCount)
                    continue;

                var atomName = particleNames[particleIndex];
                if (string.IsNullOrWhiteSpace(atomName))
                    continue;

                if (!atomNamesByResidue.TryGetValue(residueIndex,
                                                    out var atomNames))
                {
                    atomNames = new HashSet<string>(
                        StringComparer.OrdinalIgnoreCase);
                    atomNamesByResidue.Add(residueIndex, atomNames);
                }

                atomNames.Add(atomName.Trim());
            }

            return atomNamesByResidue;
        }

        private static Dictionary<int, List<int>> GetParticleIndicesByResidue(
            int[] particleResidues,
            int residueCount)
        {
            var particleIndicesByResidue =
                new Dictionary<int, List<int>>();

            if (particleResidues == null)
                return particleIndicesByResidue;

            for (var particleIndex = 0;
                 particleIndex < particleResidues.Length;
                 particleIndex++)
            {
                var residueIndex = particleResidues[particleIndex];
                if (residueIndex < 0 || residueIndex >= residueCount)
                    continue;

                if (!particleIndicesByResidue.TryGetValue(
                        residueIndex,
                        out var particleIndices))
                {
                    particleIndices = new List<int>();
                    particleIndicesByResidue.Add(residueIndex,
                                                 particleIndices);
                }

                particleIndices.Add(particleIndex);
            }

            return particleIndicesByResidue;
        }

        private static bool IsProteinResidue(
            int residueIndex,
            string residueName,
            IReadOnlyDictionary<int, HashSet<string>> atomNamesByResidue)
        {
            if (AminoAcid.IsStandardAminoAcid(residueName))
                return true;

            return atomNamesByResidue.TryGetValue(residueIndex,
                                                  out var atomNames)
                   && atomNames.Contains(BackboneNitrogen)
                   && atomNames.Contains(BackboneAlphaCarbon)
                   && atomNames.Contains(BackboneCarbonylCarbon);
        }

        private static int GetEntityIndex(int[] residueEntities,
                                          int residueIndex)
        {
            if (residueEntities == null
                || residueIndex >= residueEntities.Length
                || residueEntities[residueIndex] < -1)
                return -1;

            return residueEntities[residueIndex];
        }
    }
}
