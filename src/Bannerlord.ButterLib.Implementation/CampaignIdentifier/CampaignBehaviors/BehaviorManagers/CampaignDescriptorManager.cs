﻿using Bannerlord.ButterLib.CampaignIdentifier;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

using Path = System.IO.Path;

namespace Bannerlord.ButterLib.Implementation.CampaignIdentifier.CampaignBehaviors.BehaviorManagers
{
    [SaveableClass(2)]
    internal sealed class CampaignDescriptorManager
    {
        private const string InquiryUpperBody =
            "{=gRz8MZ0YZ7}This game has no ID. It probably was started before installing ButterLib Campaign Identifier. We have found other ongoing campaigns that have similar initial character background and were already assigned an ID. If this game refers to one of them, please, select corresponding option.";

        private const string InquiryLowerBody =
            "{=zed49rdkQR}Select '{NEW_ID}' if the loaded save is a separate campaign that has nothing to do with the suggested options and a new ID should be assigned to it.";

        private static readonly string ExistingCampaignDescriptorsLogFile = Path.Combine(Utilities.GetConfigsPath(), "ButterLib", "CampaignIdentifier", "ExistingCampaignIdentifiers.bin");


        [SaveableField(1)]
        private CampaignDescriptorImplementation _campaignDescriptor;

        private CampaignDescriptorImplementation? _descriptorToBeAssigned;

        internal CampaignDescriptorImplementation CampaignDescriptor => _campaignDescriptor;

        internal CampaignDescriptorManager()
        {
            _campaignDescriptor = null!; // Won't be null when properly accessed.
        }
        internal void GenerateNewGameDescriptor()
        {
            _campaignDescriptor = new CampaignDescriptorImplementation(Hero.MainHero);
            CampaignIdentifierEvents.Instance.OnDescriptorAssigned(_campaignDescriptor);
        }

        internal void CheckCampaignDescriptor()
        {
            // There is no guarantee that Load() will not yield a non null
            if (_campaignDescriptor is null!)
            {
                AddDescriptorToExistingCampaign();
            }
        }

        internal void UpdateCampaignDescriptor(Hero baseHero)
        {
            _campaignDescriptor = new CampaignDescriptorImplementation(_campaignDescriptor.KeyValue, baseHero);
        }

        private void AddDescriptorToExistingCampaign()
        {
            _descriptorToBeAssigned = Campaign.Current.GameStarted && Hero.All.FirstOrDefault(h => h.Id.SubId == 1) is { } baseHero
                ? new CampaignDescriptorImplementation(baseHero)
                : null;
            if (_descriptorToBeAssigned is null)
            {
                return;
            }

            var possiblyMatchingDescriptors = LoadExistingDescriptors()
                .Where(cd => cd.ImmutableKey == _descriptorToBeAssigned.ImmutableKey)
                .ToList();

            if (possiblyMatchingDescriptors.Count > 0)
            {
                var inquiryElements = possiblyMatchingDescriptors
                    .Select(existingDescriptor => new InquiryElement(
                        existingDescriptor,
                        existingDescriptor.FullCharacterName,
                        new ImageIdentifier(existingDescriptor.CharacterCode),
                        true,
                        string.Join(" - ", existingDescriptor.Descriptor, existingDescriptor.KeyValue)))
                    .ToList();

                var newIdTextObject = new TextObject("{=wF4qRrhmEu}Assign new ID");
                var inquiryBody = $"{new TextObject(InquiryUpperBody)}\n \n{new TextObject(InquiryLowerBody, new Dictionary<string, TextObject> {["NEW_ID"] = newIdTextObject})}";
                inquiryElements.Add(new InquiryElement(null, newIdTextObject.ToString(),
                    new ImageIdentifier(), true,
                    new TextObject("{=25Ts3iQnv6}This is a standalone campaign and must be assigned a new ID.").ToString()));
                InformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
                    new TextObject("{=4BuIRaILsb}Select identified ongoing campaign").ToString(),
                    inquiryBody, inquiryElements, true, 1, GameTexts.FindText("str_done").ToString(), "",
                    OnDescriptorSelectionOver,
                    OnDescriptorSelectionOver));
                return;
            }

            ApplyPredeterminedDescriptor();
        }

        private void OnDescriptorSelectionOver(List<InquiryElement> element)
        {
            if (element.Count > 0 && element[0].Identifier is CampaignDescriptorImplementation chosenDescriptor)
            {
                _descriptorToBeAssigned = chosenDescriptor;
            }

            ApplyPredeterminedDescriptor();
        }

        private void ApplyPredeterminedDescriptor()
        {
            if (_descriptorToBeAssigned is null)
            {
                return;
            }

            _campaignDescriptor = _descriptorToBeAssigned;
            CampaignIdentifierEvents.Instance.OnDescriptorAssigned(_campaignDescriptor);
        }

        private void UpdateSavedDescriptors()
        {
            var existingCampaignDescriptors = LoadExistingDescriptors();
            existingCampaignDescriptors.Add(_campaignDescriptor);

            var path = Path.GetDirectoryName(ExistingCampaignDescriptorsLogFile);
            if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
            {
                Directory.CreateDirectory(path!);
            }

            using FileStream fileStream = File.OpenWrite(ExistingCampaignDescriptorsLogFile);
            var binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fileStream, existingCampaignDescriptors);
        }

        private static List<CampaignDescriptorImplementation> LoadExistingDescriptors()
        {
            if (!File.Exists(ExistingCampaignDescriptorsLogFile))
            {
                return new List<CampaignDescriptorImplementation>();
            }

            using FileStream fileStream = File.OpenRead(ExistingCampaignDescriptorsLogFile);
            var binaryFormatter = new BinaryFormatter();
            return (List<CampaignDescriptorImplementation>)binaryFormatter.Deserialize(fileStream);
        }

        internal void Sync()
        {
            if (_descriptorToBeAssigned != null && _campaignDescriptor != null! && _descriptorToBeAssigned == _campaignDescriptor)
            {
                UpdateSavedDescriptors();
            }

            _descriptorToBeAssigned = null;
        }
    }
}