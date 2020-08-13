﻿using Bannerlord.ButterLib.Common.Helpers;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;

namespace Bannerlord.ButterLib.DistanceMatrix
{
    /// <summary>
    /// A generic class that pairs given objects of type <typeparamref name="T"/>
    /// and for each pair calculates the distance between the objects that formed it.
    /// </summary>
    /// <typeparam name="T">The type of objects for which the distance matrix should be calculated.</typeparam>
    /// <remarks><see cref="T:Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix`1" /> implements built-in calculation for the <see cref="Hero"/>,
    /// <see cref="Settlement"/>, <see cref="Clan"/> and <see cref="Kingdom"/> objects.
    /// For any other <see cref="MBObjectBase"/> subtypes custom EntityListGetter and DistanceCalculator methods
    /// should be provided using special constructor <see cref="DistanceMatrix{T}.DistanceMatrix(Func{IEnumerable{T}}, Func{T, T, float})"/> .
    /// </remarks>
    [Serializable]
    public sealed class DistanceMatrix<T> : ISerializable where T : MBObjectBase
    {
        //Fields
        private readonly Dictionary<ulong, float> _DistanceMatrix;
        [NonSerialized]
        private readonly Dictionary<(T object1, T object2), float> _TypedDistanceMatrix;
        [NonSerialized]
        private readonly Func<IEnumerable<T>>? _EntityListGetter;
        [NonSerialized]
        private readonly Func<T, T, float>? _DistanceCalculator;

        //properties
        /// <summary>Raw distance matrix representation</summary>
        /// <value>
        /// A dictionary of paired type <typeparamref name="T"/> objects
        /// represented by unique 64-bit unsigned number as key
        /// and floating-point numbers, representing distances between those objects as value.
        /// </value>
        public Dictionary<ulong, float> AsDictionary => _DistanceMatrix;

        /// <summary>Objectified distance matrix representation</summary>
        /// <value>
        /// A dictionary of paired type <typeparamref name="T"/> objects as key
        /// and floating-point numbers, representing distances between those objects as value.
        /// </value>
        public Dictionary<(T object1, T object2), float> AsTypedDictionary => _TypedDistanceMatrix;

        //Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="T:Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix`1"/> class
        /// with default EntityListGetter and DistanceCalculator methods.
        /// </summary>
        /// <exception cref="T:System.ArgumentException"></exception>
        public DistanceMatrix()
        {
            _EntityListGetter = null;
            _DistanceCalculator = null;
            _DistanceMatrix = CalculateDistanceMatrix();
            _TypedDistanceMatrix = GetTypedDistanceMatrix();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix`1"/> class
        /// with custom methods that will be used to get the list of analyzed objects and calculate the distances between them.
        /// </summary>
        /// <param name="customListGetter">
        /// A delegate to the method that will be used to get a list of objects of type <typeparamref name="T"/>
        /// for calculating the distances between them.
        /// </param>
        /// <param name="customDistanceCalculator">
        /// A delegate to the method that will be used to calculate the distance between two given type <typeparamref name="T"/> objects.
        /// </param>
        /// <exception cref="T:System.ArgumentException"></exception>
        public DistanceMatrix(Func<IEnumerable<T>> customListGetter, Func<T, T, float> customDistanceCalculator)
        {
            _EntityListGetter = customListGetter;
            _DistanceCalculator = customDistanceCalculator;
            _DistanceMatrix = CalculateDistanceMatrix();
            _TypedDistanceMatrix = GetTypedDistanceMatrix();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Bannerlord.ButterLib.DistanceMatrix.DistanceMatrix`1" /> class
        /// using serialized data.
        /// </summary>
        /// <remarks>Used exclusively for deserialization.</remarks>
        private DistanceMatrix(SerializationInfo info, StreamingContext context)
        {
            Type type = Type.GetType(info.GetString("ObjectTypeName"));
            _DistanceMatrix = (Dictionary<ulong, float>)info.GetValue(type.Name + "DistanceMatrix", typeof(Dictionary<ulong, float>));
            _DistanceMatrix.OnDeserialization(this);
            _TypedDistanceMatrix = GetTypedDistanceMatrix();
        }

        //Public methods
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ObjectTypeName", typeof(T).AssemblyQualifiedName);
            info.AddValue(typeof(T).Name + "DistanceMatrix", _DistanceMatrix);
        }

        /// <summary>Gets calculated distance between specified type <typeparamref name="T"/> objects.</summary>
        /// <param name="object1">The first of the objects between which it is necessary to determine the distance.</param>
        /// <param name="object2">The second of the objects between which it is necessary to determine the distance.</param>
        /// <returns>
        /// A floating-point number representing the distance between two specified <see cref="MBObjectBase"/> objects;
        /// or <see cref="float.NaN" />, if distance was not calculated or it is uncomputable.
        /// </returns>
        public float GetDistance(T object1, T object2) =>
            _DistanceMatrix.TryGetValue(object1.Id > object2.Id ? ElegantPairHelper.Pair(object2.Id, object1.Id) : ElegantPairHelper.Pair(object1.Id, object2.Id),
                                        out float distance) ? distance : float.NaN;

        /// <summary>Calculates distance between two given <see cref="Hero"/> objects.</summary>
        /// <param name="hero1">The first of the heroes to calculate distance between.</param>
        /// <param name="hero2">The second of the heroes to calculate distance between.</param>
        /// <returns>
        /// A floating-point number representing the distance between two specified <see cref="Hero"/> objects
        /// or <see cref="float.NaN" /> if distance could not be calculated.
        /// </returns>
        public static float CalculateDistanceBetweenHeroes(Hero hero1, Hero hero2)
        {
            (MobileParty? mobileParty1, Settlement? settlement1) = GetMapPosition(hero1);
            (MobileParty? mobileParty2, Settlement? settlement2) = GetMapPosition(hero2);

            return
              settlement1 != null && settlement2 != null
              ? Campaign.Current.Models.MapDistanceModel.GetDistance(settlement1, settlement2)
              : mobileParty1 != null && mobileParty2 != null
              ? Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty1, mobileParty2)
              : settlement1 != null && mobileParty2 != null
              ? Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty2, settlement1)
              : mobileParty1 != null && settlement2 != null
              ? Campaign.Current.Models.MapDistanceModel.GetDistance(mobileParty1, settlement2)
              : float.NaN;
        }

        /// <summary>Calculates distance between two given <see cref="Clan"/> objects.</summary>
        /// <param name="clan1">First of the clans to calculate distance between.</param>
        /// <param name="clan2">Second of the clans to calculate distance between.</param>
        /// <param name="settlementOwnersPairedList">
        /// List of the distances between pairs of settlements and of the weights of the paired settlements,
        /// except that the owner clan pairs are used instead of the settlements themselves to speed up the process.
        /// </param>
        /// <returns>
        /// A floating-point number representing the distance between two specified <see cref="Clan"/> objects
        /// or <see cref="float.NaN" /> if distance could not be calculated.
        /// </returns>
        /// <remarks>Calculation is based on the average distance between clans fiefs weighted by the fief type.</remarks>
        public static float CalculateDistanceBetweenClans(Clan clan1, Clan clan2, List<(ulong owners, float distance, float weight)> settlementOwnersPairedList)
        {
            ulong pair = clan1.Id > clan2.Id ? ElegantPairHelper.Pair(clan2.Id, clan1.Id) : ElegantPairHelper.Pair(clan1.Id, clan2.Id);
            List<(float distance, float weight)> settlementDistances = settlementOwnersPairedList.Where(tuple => tuple.owners == pair && !float.IsNaN(tuple.distance))
                                                                                                 .Select(x => (x.distance, x.weight)).ToList();
            return GetWeightedMeanDistance(settlementDistances);
        }

        /// <summary>Calculates distance between two given <see cref="Kingdom"/> objects.</summary>
        /// <param name="kingdom1">First of the kingdoms to calculate distance between.</param>
        /// <param name="kingdom2">Second of the kingdoms to calculate distance between.</param>
        /// <param name="settlementDistanceMatrix">Settlement distance matrix .</param>
        /// <returns>
        /// A floating-point number representing the distance between two specified <see cref="Kingdom"/> objects
        /// or <see cref="float.NaN" /> if distance could not be calculated.
        /// </returns>
        /// <remarks>Calculation is based on the average distance between kingdoms fiefs weighted by the fief type.</remarks>
        public static float CalculateDistanceBetweenKingdoms(Kingdom kingdom1, Kingdom kingdom2, DistanceMatrix<Settlement> settlementDistanceMatrix)
        {
            bool predicate(KeyValuePair<(Settlement object1, Settlement object2), float> x) =>
              x.Value != float.NaN && ((x.Key.object1.MapFaction == kingdom1 && x.Key.object2.MapFaction == kingdom2)
                                       || (x.Key.object2.MapFaction == kingdom1 && x.Key.object1.MapFaction == kingdom2));

            static (float distance, float weight) WeightedSettlementSelector(KeyValuePair<(Settlement object1, Settlement object2), float> x) =>
              (x.Value, GetSettlementWeight(x.Key.object1) + GetSettlementWeight(x.Key.object2));

            List<(float distance, float weight)> settlementDistances = settlementDistanceMatrix.AsTypedDictionary.Where(predicate).Select(WeightedSettlementSelector).ToList();
            return GetWeightedMeanDistance(settlementDistances);
        }

        /// <summary>
        /// Transforms given <see cref="Settlement"/> distance matrix into list of the weighted distances
        /// between pairs of settlements, except that the owner clan pairs are used instead of the settlements themselves.
        /// </summary>
        /// <param name="settlementDistanceMatrix">Settlement distance matrix to transform into list.</param>
        /// <returns>
        /// A list of tuples holding information about pair of initial settlements owners, distance between settlements
        /// and combined settlement weight.
        /// </returns>
        /// <remarks>
        /// This method could be used to supply
        /// <see cref="DistanceMatrix{T}.CalculateDistanceBetweenClans(Clan, Clan, List{(ulong owners, float distance, float weight)})"/>
        /// method with required list argument.
        /// </remarks>
        public static List<(ulong owners, float distance, float weight)> GetSettlementOwnersPairedList(DistanceMatrix<Settlement> settlementDistanceMatrix)
        {
            static (MBGUID ownerId1, MBGUID ownerId2, float value, float weight) firstSelector(KeyValuePair<(Settlement object1, Settlement object2), float> kvp) =>
              (ownerId1: kvp.Key.object1.OwnerClan.Id, ownerId2: kvp.Key.object2.OwnerClan.Id, value: kvp.Value, weight: GetSettlementWeight(kvp.Key.object1) + GetSettlementWeight(kvp.Key.object2));

            static (ulong, float value, float weight) secondSelector((MBGUID ownerId1, MBGUID ownerId2, float value, float weight) tuple) =>
              (tuple.ownerId1 > tuple.ownerId2 ? ElegantPairHelper.Pair(tuple.ownerId2, tuple.ownerId1) : ElegantPairHelper.Pair(tuple.ownerId1, tuple.ownerId2), tuple.value, tuple.weight);

            List<(ulong owners, float distance, float weight)> lst = settlementDistanceMatrix.AsTypedDictionary.Select(firstSelector).Select(secondSelector).ToList();
            return lst;
        }

        //Private methods
        /// <summary>Calculates distance matrix for the <see cref="MBObjectBase"/> objects of the specified subtype <typeparamref name="T"/>.</summary>
        /// <exception cref="T:System.ArgumentException"></exception>
        private Dictionary<ulong, float> CalculateDistanceMatrix()
        {
            if (_EntityListGetter != null && _DistanceCalculator != null)
            {
                IEnumerable<T> entities = _EntityListGetter();
                return entities.SelectMany(x => entities, (x, y) => (x, y)).Where(tuple => tuple.x.Id < tuple.y.Id)
                               .ToDictionary(key => ElegantPairHelper.Pair(key.x.Id, key.y.Id), value => _DistanceCalculator(value.x, value.y));
            }
            if (typeof(T).IsAssignableFrom(typeof(Hero)))
            {
                List<Hero> activeHeroes = Hero.All.Where(h => h.IsInitialized && !h.IsNotSpawned && !h.IsDisabled && !h.IsDead && !h.IsNotable).ToList();
                return activeHeroes.SelectMany(x => activeHeroes, (x, y) => (x, y)).Where(tuple => tuple.x.Id < tuple.y.Id)
                                   .ToDictionary(key => ElegantPairHelper.Pair(key.x.Id, key.y.Id), value => CalculateDistanceBetweenHeroes(value.x, value.y));
            }
            if (typeof(T).IsAssignableFrom(typeof(Settlement)))
            {
                List<Settlement> settlements = Settlement.All.Where(s => s.IsInitialized && (s.IsFortification || s.IsVillage)).ToList();
                return settlements.SelectMany(x => settlements, (x, y) => (x, y)).Where(tuple => tuple.x.Id < tuple.y.Id)
                                  .ToDictionary(key => ElegantPairHelper.Pair(key.x.Id, key.y.Id), value => Campaign.Current.Models.MapDistanceModel.GetDistance(value.x, value.y));
            }
            if (typeof(T).IsAssignableFrom(typeof(Clan)))
            {
                List<Clan> clans = Clan.All.Where(c => c.IsInitialized && c.Fortifications.Count > 0).ToList();
                DistanceMatrix<Settlement> settlementDistanceMatrix = Campaign.Current.GetCampaignBehavior<GeopoliticsCachingBehavior>().SettlementDistanceMatrix ?? new DistanceMatrix<Settlement>();
                List<(ulong owners, float distance, float weight)> lst = GetSettlementOwnersPairedList(settlementDistanceMatrix);

                return clans.SelectMany(x => clans, (x, y) => (x, y)).Where(tuple => tuple.x.Id < tuple.y.Id)
                            .ToDictionary(key => ElegantPairHelper.Pair(key.x.Id, key.y.Id), value => CalculateDistanceBetweenClans(value.x, value.y, lst));
            }
            if (typeof(T).IsAssignableFrom(typeof(Kingdom)))
            {
                List<Kingdom> kingdoms = Kingdom.All.Where(k => k.IsInitialized && k.Fiefs.Any()).ToList();
                DistanceMatrix<Settlement> settlementDistanceMatrix = Campaign.Current.GetCampaignBehavior<GeopoliticsCachingBehavior>().SettlementDistanceMatrix ?? new DistanceMatrix<Settlement>();
                return kingdoms.SelectMany(x => kingdoms, (x, y) => (x, y)).Where(tuple => tuple.x.Id < tuple.y.Id)
                               .ToDictionary(key => ElegantPairHelper.Pair(key.x.Id, key.y.Id), value => CalculateDistanceBetweenKingdoms(value.x, value.y, settlementDistanceMatrix));
            }
            throw new ArgumentException(string.Format("{0} is not supported type", typeof(string).FullName));
        }

        private static (MobileParty? mobileParty, Settlement? settlement) GetMapPosition(Hero hero) =>
            hero.IsPrisoner && hero.PartyBelongedToAsPrisoner != null
                ? hero.PartyBelongedToAsPrisoner.IsSettlement ? ((MobileParty?)null, hero.PartyBelongedToAsPrisoner.Settlement) : (hero.PartyBelongedToAsPrisoner.MobileParty, (Settlement?)null)
                : hero.CurrentSettlement != null && !hero.IsFugitive
                ? ((MobileParty?)null, hero.CurrentSettlement)
                : hero.PartyBelongedTo != null ? (hero.PartyBelongedTo, (Settlement?)null) : (null, null);

        private static float GetWeightedMeanDistance(List<(float distance, float weight)> settlementDistances) =>
            settlementDistances.Any(x => x.weight > 0)
                ? (float)((settlementDistances.Sum(x => x.distance * x.weight) + 1.0) / (settlementDistances.Sum(x => x.weight) + 1.0))
                : float.NaN;

        private static float GetSettlementWeight(Settlement settlement) => settlement.IsTown ? 2f : settlement.IsCastle ? 1f : settlement.IsVillage ? 0.5f : 0f;

        private Dictionary<(T object1, T object2), float> GetTypedDistanceMatrix() =>
            _DistanceMatrix.ToDictionary(key => ElegantPairHelper.UnPairMBGUID(key.Key), value => value.Value)
                           .ToDictionary(key => ((T)MBObjectManager.Instance.GetObject(key.Key.a), (T)MBObjectManager.Instance.GetObject(key.Key.b)), value => value.Value);
    }
}
