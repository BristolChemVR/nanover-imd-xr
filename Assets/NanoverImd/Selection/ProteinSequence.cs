using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NanoverImd.Selection
{
    /// <summary>
    /// Immutable protein-sequence topology shared by all residue selections for
    /// one simulation.
    /// </summary>
    public sealed class ProteinSequence
    {
        private static readonly IReadOnlyList<ProteinResidueInfo> EmptyResidues =
            new ProteinResidueInfo[0];

        private readonly IReadOnlyList<ProteinResidueInfo> residues;
        private readonly IReadOnlyList<int> entityIndices;
        private readonly IReadOnlyCollection<int> residueIndices;
        private readonly Dictionary<int, ProteinResidueInfo> residuesByIndex;
        private readonly Dictionary<int, IReadOnlyList<ProteinResidueInfo>>
            residuesByEntity;

        /// <summary>
        /// All protein residues, ordered first by entity and then by residue
        /// index within that entity.
        /// </summary>
        public IReadOnlyList<ProteinResidueInfo> Residues => residues;

        /// <summary>
        /// Entity indices represented in this sequence, in ascending order.
        /// </summary>
        public IReadOnlyList<int> EntityIndices => entityIndices;

        /// <summary>
        /// The number of residues in this sequence.
        /// </summary>
        public int Count => residues.Count;

        /// <summary>
        /// Residue indices represented in this sequence, in the same order as
        /// <see cref="Residues"/>.
        /// </summary>
        public IReadOnlyCollection<int> ResidueIndices => residueIndices;

        public ProteinSequence(IEnumerable<ProteinResidueInfo> residues)
        {
            if (residues == null)
                throw new ArgumentNullException(nameof(residues));

            var residuesByIndex = new Dictionary<int, ProteinResidueInfo>();
            foreach (var residue in residues)
            {
                if (residue == null)
                    throw new ArgumentException(
                        "The sequence cannot contain a null residue.",
                        nameof(residues));

                if (residuesByIndex.ContainsKey(residue.ResidueIndex))
                    throw new ArgumentException(
                        $"Duplicate residue index {residue.ResidueIndex}.",
                        nameof(residues));

                residuesByIndex.Add(residue.ResidueIndex, residue);
            }

            var orderedResidues = residuesByIndex.Values
                                                 .OrderBy(residue => residue.EntityIndex)
                                                 .ThenBy(residue => residue.ResidueIndex)
                                                 .ToList();

            this.residuesByIndex = residuesByIndex;
            this.residues = new ReadOnlyCollection<ProteinResidueInfo>(orderedResidues);

            var entityIndices = orderedResidues.Select(residue => residue.EntityIndex)
                                               .Distinct()
                                               .ToList();
            this.entityIndices = new ReadOnlyCollection<int>(entityIndices);

            var residueIndices = orderedResidues.Select(residue => residue.ResidueIndex)
                                                .ToList();
            this.residueIndices = new ReadOnlyCollection<int>(residueIndices);

            residuesByEntity = new Dictionary<int, IReadOnlyList<ProteinResidueInfo>>();
            foreach (var entityGroup in orderedResidues.GroupBy(residue => residue.EntityIndex))
            {
                var entityResidues = entityGroup.ToList();
                residuesByEntity.Add(
                    entityGroup.Key,
                    new ReadOnlyCollection<ProteinResidueInfo>(entityResidues));
            }
        }

        /// <summary>
        /// Finds a residue using its frame residue index.
        /// </summary>
        public bool TryGetResidue(int residueIndex,
                                  out ProteinResidueInfo residue)
        {
            return residuesByIndex.TryGetValue(residueIndex, out residue);
        }

        /// <summary>
        /// Returns the ordered residues for an entity, or an empty read-only
        /// list if that entity is not represented in this sequence.
        /// </summary>
        public IReadOnlyList<ProteinResidueInfo> GetResiduesForEntity(int entityIndex)
        {
            return residuesByEntity.TryGetValue(entityIndex, out var entityResidues)
                ? entityResidues
                : EmptyResidues;
        }

        /// <summary>
        /// Returns the distinct particle indices belonging to the specified
        /// residues, in ascending order.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when a residue index is not part of this protein sequence.
        /// </exception>
        public IReadOnlyList<int> GetParticleIndicesForResidues(
            IEnumerable<int> residueIndices)
        {
            if (residueIndices == null)
                throw new ArgumentNullException(nameof(residueIndices));

            var particleIndices = new SortedSet<int>();
            foreach (var residueIndex in residueIndices)
            {
                if (!residuesByIndex.TryGetValue(residueIndex,
                                                 out var residue))
                    throw new ArgumentOutOfRangeException(
                        nameof(residueIndices),
                        residueIndex,
                        "The residue is not part of this protein sequence.");

                particleIndices.UnionWith(residue.ParticleIndices);
            }

            return new ReadOnlyCollection<int>(particleIndices.ToList());
        }

        /// <summary>
        /// Returns the one-letter sequence for an entity. Non-standard residues
        /// are represented by X.
        /// </summary>
        public string GetSequenceCodeForEntity(int entityIndex)
        {
            var entityResidues = GetResiduesForEntity(entityIndex);
            var sequence = new char[entityResidues.Count];

            for (var i = 0; i < entityResidues.Count; i++)
                sequence[i] = entityResidues[i].SequenceCode;

            return new string(sequence);
        }
    }
}
