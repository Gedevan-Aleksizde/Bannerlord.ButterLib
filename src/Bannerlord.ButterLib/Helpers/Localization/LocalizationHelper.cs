﻿using Bannerlord.BUTR.Shared.Helpers;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

// ReSharper disable once CheckNamespace
namespace Bannerlord.ButterLib.Common.Helpers
{
    /// <summary>Helper class used to store numeric variables or complex entities in the <see cref="TextObject" />.</summary>
    public static class LocalizationHelper
    {
        /// <summary>
        /// A string tag that corresponds with <see cref="PluralForm.Plural" />
        /// and could be stored as an attribute of the numeric variable by the "SetNumericVariable" method.
        /// </summary>
        public const string PLURAL_FORM_TAG = "PLURAL_FORM";

        /// <summary>
        /// A string tag that corresponds with <see cref="PluralForm.SpecificSingular" />
        /// and could be stored as an attribute of the numeric variable by the "SetNumericVariable" method.
        /// </summary>
        public const string SPECIFIC_SINGULAR_FORM_TAG = "SPECIFIC_SINGULAR_FORM";

        /// <summary>
        /// A string tag that corresponds with <see cref="PluralForm.SpecificPlural" />
        /// and could be stored as an attribute of the numeric variable by the "SetNumericVariable" method.
        /// </summary>
        public const string SPECIFIC_PLURAL_FORM_TAG = "SPECIFIC_PLURAL_FORM";

        /// <summary>
        /// A string tag used in <see cref="SetListVariable" /> method.
        /// Indicates that list has at least 1 element.
        /// </summary>
        public const string LIST_HAS_ITEMS_TAG = "ANY";

        /// <summary>
        /// A string tag used in <see cref="SetListVariable" /> method.
        /// Indicates that list has more than 1 elements.
        /// </summary>
        public const string LIST_HAS_MULTIPLE_ITEMS_TAG = "IS_PLURAL";

        /// <summary>
        /// A string tag used in <see cref="SetListVariable" /> method.
        /// Will contain all the elements of the list except the last one, separated by a comma.
        /// For empty lists or lists with a single element it will contain an empty string.
        /// </summary>
        public const string LIST_START_TAG = "START";

        /// <summary>
        /// A string tag used in <see cref="SetListVariable" /> method.
        /// Will contain the last element of the list.
        /// </summary>
        public const string LIST_END_TAG = "END";

        private const string BUILT_IN_AGGREGATION_STRING = "{=JLkq0ZkOSI}{START}{?IS_PLURAL} and {?}{\\?}{END}";

        private static readonly ReadOnlyCollection<int> EasternSlavicPluralExceptions = new(new List<int> { 11, 12, 13, 14 });
        private static readonly ReadOnlyCollection<int> EasternSlavicSingularNumerics = new(new List<int> { 1, 2, 3, 4 });

        private static readonly ReadOnlyCollection<int> WesternSlavicPluralExceptions = new(new List<int> { 12, 13, 14 });
        private static readonly ReadOnlyCollection<int> WesternSlavicSingularNumerics = new(new List<int> { 2, 3, 4 });

        private static readonly ReadOnlyCollection<string> SingleZeroLanguageIDs = new(new List<string> { "French", "Français" });

        private static readonly ReadOnlyCollection<string> EasternSlavicGroupLanguageIDs = new(new List<string> { "Russian", "Русский", "Ukrainian", "Українська", "Belarusian", "Беларускі" });
        private static readonly ReadOnlyCollection<string> WesternSlavicGroupLanguageIDs = new(new List<string> { "Polish", "Polski" });

        private static RecursiveCaller GetRecursiveCaller(RecursiveCaller currentCaller, RecursiveCaller receivedCaller)
        {
            return (RecursiveCaller) Math.Max((byte) currentCaller, (byte) receivedCaller);
        }

        private static RecursiveCaller GetCurrentCaller<T>(T entity) where T : class
        {
            return entity switch
            {
                Hero _ => RecursiveCaller.Hero,
                Settlement _ => RecursiveCaller.Settlement,
                Clan _ => RecursiveCaller.Clan,
                Kingdom _ => RecursiveCaller.Kingdom,
                _ => throw new ArgumentException($"{entity.GetType().FullName} is not supported type", nameof(entity)),
            };
        }

        private static TextObject GetEntityTextObject<T>(T entity) where T : class
        {
            switch (entity)
            {
                case Hero hero:
                    var characterProperties = new TextObject(string.Empty);
                    characterProperties.SetTextVariable("NAME", hero.Name);
                    characterProperties.SetTextVariable("AGE", (int) hero.Age);
                    characterProperties.SetTextVariable("GENDER", hero.IsFemale ? 1 : 0);
                    characterProperties.SetTextVariable("LINK", hero.EncyclopediaLinkWithName);
                    characterProperties.SetTextVariable("FIRSTNAME", hero.FirstName ?? hero.Name);
                    return characterProperties;
                case Settlement settlement:
                    var settlementProperties = new TextObject(string.Empty);
                    settlementProperties.SetTextVariable("NAME", settlement.Name);
                    settlementProperties.SetTextVariable("IS_TOWN", settlement.IsTown ? 1 : 0);
                    settlementProperties.SetTextVariable("IS_CASTLE", settlement.IsCastle ? 1 : 0);
                    settlementProperties.SetTextVariable("IS_VILLAGE", settlement.IsVillage ? 1 : 0);
                    settlementProperties.SetTextVariable("LINK", settlement.EncyclopediaLinkWithName);
                    return settlementProperties;
                case Clan clan:
                    var clanProperties = new TextObject(string.Empty);
                    clanProperties.SetTextVariable("NAME", clan.Name);
                    clanProperties.SetTextVariable("MINOR_FACTION", clan.IsMinorFaction ? 1 : 0);
                    clanProperties.SetTextVariable("UNDER_CONTRACT", clan.IsUnderMercenaryService ? 1 : 0);
                    clanProperties.SetTextVariable("LINK", clan.EncyclopediaLinkWithName);
                    return clanProperties;
                case Kingdom kingdom:
                    var kingdomProperties = new TextObject(string.Empty);
                    kingdomProperties.SetTextVariable("NAME", kingdom.Name);
                    kingdomProperties.SetTextVariable("LINK", kingdom.EncyclopediaLinkWithName);
                    return kingdomProperties;
                default:
                    throw new ArgumentException($"{entity.GetType().FullName} is not supported type", nameof(entity));
            }
        }

        private static void SetRelatedProperties<T>(TextObject? parentTextObject, string tag, T entity, bool addLeaderInfo, RecursiveCaller recursiveCaller) where T : class
        {
            switch (entity)
            {
                case Hero hero:
                    SetEntityProperties(parentTextObject, tag + "_CLAN", hero.Clan, addLeaderInfo, GetRecursiveCaller(RecursiveCaller.Hero, recursiveCaller));
                    break;
                case Settlement settlement:
                    SetEntityProperties(parentTextObject, tag + "_CLAN", settlement.OwnerClan, addLeaderInfo, GetRecursiveCaller(RecursiveCaller.Settlement, recursiveCaller));
                    break;
                case Clan clan:
                    if (addLeaderInfo)
                    {
                        SetEntityProperties(parentTextObject, tag + "_LEADER", clan.Leader, false, RecursiveCaller.Clan);
                    }
                    if (clan.Kingdom is not null)
                    {
                        SetEntityProperties(parentTextObject, tag + "_KINGDOM", clan.Kingdom, addLeaderInfo, GetRecursiveCaller(RecursiveCaller.Clan, recursiveCaller));
                    }
                    break;
                case Kingdom kingdom:
                    if (addLeaderInfo)
                    {
                        SetEntityProperties(parentTextObject, tag + "_LEADER", kingdom.Leader, false, RecursiveCaller.Kingdom);
                    }
                    break;
                default:
                    throw new ArgumentException($"{entity.GetType().FullName} is not supported type", nameof(entity));
            }
        }


        /// <summary>Sets complex entity into specified text variable, along with additional information on other related entities.</summary>
        /// <typeparam name="T">The type of the entity to be stored.</typeparam>
        /// <param name="parentTextObject">
        /// The <see cref="TextObject" /> to store entity information into.
        /// Null means that information will be stored into <see cref="MBTextManager" />.
        /// </param>
        /// <param name="tag">A string tag that will be used to store information about entity and also as a prefix for tags that will store other relevant entities.</param>
        /// <param name="entity">An instance of the entity to be stored.</param>
        /// <param name="addLeaderInfo">An optional argument, specifying if information about leaders should be also stored, when applicable.</param>
        /// <param name="recursiveCaller">An optional argument, specifying if method is called from itself, adding information on some related entity.</param>
        public static void SetEntityProperties<T>(TextObject? parentTextObject, string tag, T? entity, bool addLeaderInfo = false, RecursiveCaller recursiveCaller = RecursiveCaller.None) where T : class
        {
            if (string.IsNullOrEmpty(tag) || entity is null || recursiveCaller == GetCurrentCaller(entity))
            {
                return;
            }
            if (parentTextObject is null)
            {
                MBTextManager.SetTextVariable(tag, GetEntityTextObject(entity));
            }
            else
            {
                parentTextObject.SetTextVariable(tag, GetEntityTextObject(entity));
            }
            SetRelatedProperties(parentTextObject, tag, entity, addLeaderInfo, recursiveCaller);
        }

        private static PluralForm GetEasternSlavicPluralFormInternal(int number)
        {
            var absNumber = Math.Abs(number);
            var lastDigit = absNumber % 10;
            return
              EasternSlavicPluralExceptions.Contains(absNumber % 100) || !EasternSlavicSingularNumerics.Contains(lastDigit)
                ? PluralForm.Plural
                : !EasternSlavicPluralExceptions.Contains(absNumber) && EasternSlavicSingularNumerics.Contains(lastDigit) && lastDigit != 1
                ? PluralForm.SpecificSingular : PluralForm.Singular;
        }

        private static PluralForm GetWesternSlavicPluralFormInternal(int number)
        {
            var absNumber = Math.Abs(number);
            var lastDigit = absNumber % 10;
            return
              absNumber > 1 && (WesternSlavicPluralExceptions.Contains(absNumber % 100) || !WesternSlavicSingularNumerics.Contains(lastDigit))
                ? PluralForm.Plural
                : !WesternSlavicPluralExceptions.Contains(absNumber) && WesternSlavicSingularNumerics.Contains(lastDigit)
                ? PluralForm.SpecificPlural : PluralForm.Singular;
        }

        /// <summary>Gets which <see cref="PluralForm" /> should be used with the given number according to the grammar rules of the game language.</summary>
        /// <param name="number">An integer number to get appropriate <see cref="PluralForm" /> for.</param>
        /// <returns>The appropriate <see cref="PluralForm" /> that should be used with the given number in accordance with the grammar rules of the game language.</returns>
        public static PluralForm GetPluralForm(int number)
        {
            if (EasternSlavicGroupLanguageIDs.Contains(BannerlordConfig.Language))
            {
                return GetEasternSlavicPluralFormInternal(number);
            }
            if (WesternSlavicGroupLanguageIDs.Contains(BannerlordConfig.Language))
            {
                return GetWesternSlavicPluralFormInternal(number);
            }
            if (SingleZeroLanguageIDs.Contains(BannerlordConfig.Language) && number == 0)
            {
                return PluralForm.Singular;
            }
            return number != 1 ? PluralForm.Plural : PluralForm.Singular;
        }

        /// <summary>Gets which <see cref="PluralForm" /> should be used with the given number according to the grammar rules of the game language.</summary>
        /// <param name="number">A floating-point number to get appropriate <see cref="PluralForm" /> for.</param>
        /// <returns>The appropriate <see cref="PluralForm" /> that should be used with the given number in accordance with the grammar rules of the game language.</returns>
        public static PluralForm GetPluralForm(float number)
        {
            if (EasternSlavicGroupLanguageIDs.Contains(BannerlordConfig.Language))
            {
                return GetEasternSlavicPluralFormInternal((int) Math.Floor(number));
            }
            return Math.Abs(number) > 1f ? PluralForm.Plural : PluralForm.Singular; //Language Plural Rules for floating numbers are actually much more complicated than that, but not worth the effort. For now.
        }

        private static Dictionary<string, object> GetPluralFormAttributes(PluralForm pluralForm) =>
            new()
            {
                [PLURAL_FORM_TAG] = new TextObject(pluralForm == PluralForm.Plural ? 1 : 0),
                [SPECIFIC_PLURAL_FORM_TAG] = new TextObject(pluralForm == PluralForm.SpecificPlural ? 1 : 0),
                [SPECIFIC_SINGULAR_FORM_TAG] = new TextObject(pluralForm == PluralForm.SpecificSingular ? 1 : 0)
            };

        /// <summary>Sets a numeric variable along with the appropriate <see cref="PluralForm" /> tag in accordance with the grammar rules of the game language.</summary>
        /// <param name="textObject">
        /// The <see cref="TextObject" /> to set a numeric variable into.
        /// Null means that information will be stored into <see cref="MBTextManager" />.
        /// </param>
        /// <param name="tag">A string tag that will be used to store information about the numeric variable.</param>
        /// <param name="variableValue">An integer number to be set.</param>
        /// <param name="format">An optional argument, specifying string format to be used with the number.</param>
        public static void SetNumericVariable(TextObject? textObject, string tag, int variableValue, string? format = null)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            var attributes = GetPluralFormAttributes(GetPluralForm(variableValue));
            var explainedTextObject = string.IsNullOrEmpty(format)
                ? new TextObject(variableValue.ToString(), attributes)
                : new TextObject(variableValue.ToString(format), attributes);
            if (textObject is null)
            {
                MBTextManager.SetTextVariable(tag, explainedTextObject);
            }
            else
            {
                textObject.SetTextVariable(tag, explainedTextObject);
            }
        }

        /// <summary>Sets a numeric variable along with the appropriate <see cref="PluralForm" /> tag in accordance with the grammar rules of the game language.</summary>
        /// <param name="textObject">
        /// The <see cref="TextObject" /> to set a numeric variable into.
        /// Null means that information will be stored into <see cref="MBTextManager" />.
        /// </param>
        /// <param name="tag">A string tag that will be used to store information about the numeric variable.</param>
        /// <param name="variableValue">An floating-point number to be set.</param>
        /// <param name="format">An optional argument, specifying string format to be used with the number.</param>
        public static void SetNumericVariable(TextObject? textObject, string tag, float variableValue, string? format = null)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            var attributes = GetPluralFormAttributes(GetPluralForm(variableValue));
            var explainedTextObject = string.IsNullOrEmpty(format)
                ? new TextObject(variableValue.ToString("R"), attributes)
                : new TextObject(variableValue.ToString(format), attributes);
            if (textObject is null)
            {
                MBTextManager.SetTextVariable(tag, explainedTextObject);
            }
            else
            {
                textObject.SetTextVariable(tag, explainedTextObject);
            }
        }

        private static Dictionary<string, object?> GetListAttributes(IEnumerable<string> valuesList, string separator = ", ", bool useDistinctValues = true)
        {
            var localValues = (useDistinctValues ? valuesList.Distinct() : valuesList).ToList();
            return localValues.Any()
                ? (new()
                {
                    [LIST_HAS_ITEMS_TAG] = new TextObject(1),
                    [LIST_HAS_MULTIPLE_ITEMS_TAG] = new TextObject(localValues.Count != 1 ? 1 : 0),
                    [LIST_START_TAG] = new TextObject(string.Join(separator, localValues.Take(localValues.Count - 1))),
                    [LIST_END_TAG] = new TextObject(localValues.Last())
                })
                : (new()
                {
                    [LIST_HAS_ITEMS_TAG] = new TextObject(0),
                    [LIST_HAS_MULTIPLE_ITEMS_TAG] = new TextObject(0),
                    [LIST_START_TAG] = new TextObject(string.Empty),
                    [LIST_END_TAG] = new TextObject(string.Empty)
                });
        }

        /// <summary>Sets a variable containing a list of strings to the specified tag so that all elements are correctly listed according to the grammar rules of the game language.</summary>
        /// <param name="textObject">
        /// The <see cref="TextObject" /> to set a <see cref="List{T}" /> variable into.
        /// Null means that information will be stored into <see cref="MBTextManager" />.
        /// </param>
        /// <param name="tag">A string tag that will be used to store information about the strings in the provided list.</param>
        /// <param name="valuesList">A set of strings that has to be listed.</param>
        /// <param name="separator">An optional argument specifying the string separator to be used for the listing.</param>
        /// <param name="useDistinctValues">An optional argument specifying whether only unique strings should be listed.</param>
        /// <remarks>
        /// The tag parameter will contain a <see cref="TextObject" /> with both the ready-to-use result of the built-in concatenation of the provided list of strings
        /// and all the necessary attributes to create custom concatenations.
        /// Please note that built-in concatenation uses the "{=JLkq0ZkOSI}{START}{?IS_PLURAL} and {?}{\\?}{END}" string from the "..\Bannerlord.ButterLib\ModuleData\Languages\EN\sta_strings.xml".
        /// If you are planning to use it explicitly, it is advised to duplicate it in your own localization xml files for the sake of clarity.
        /// </remarks>
        /// <example>
        /// <code>
        /// var lst = new [] { "First Entry", "Second Entry", "Third Entry", "Second Entry" }.ToList();
        /// var txt = new TextObject("Here is the built-in text: '{TEST_TAG}'. Here is custom text: '{TEST_TAG.START}{?TEST_TAG.IS_PLURAL} and last but not least the {?}{\\?}{TEST_TAG.END}'");
        /// LocalizationHelper.SetListVariable(txt, "TEST_TAG", lst);
        /// InformationManager.DisplayMessage(new InformationMessage(txt!.ToString(), Color.FromUint(0x00F16D26)));
        /// </code>
        /// </example>
        public static void SetListVariable(TextObject? textObject, string tag, List<string> valuesList, string separator = ", ", bool useDistinctValues = true)
        {
            if (string.IsNullOrEmpty(tag))
            {
                return;
            }
            var attributes = GetListAttributes(valuesList, separator, useDistinctValues);
            var explainedTextObject = new TextObject(BUILT_IN_AGGREGATION_STRING, attributes);
            if (textObject is null)
            {
                MBTextManager.SetTextVariable(tag, explainedTextObject);
            }
            else
            {
                textObject.SetTextVariable(tag, explainedTextObject);
            }
        }

        public enum RecursiveCaller : byte
        {
            None,
            Hero,
            Settlement,
            Clan,
            Kingdom
        }
        public enum PluralForm : byte
        {
            Singular,
            SpecificSingular,
            SpecificPlural,
            Plural
        }
    }
}