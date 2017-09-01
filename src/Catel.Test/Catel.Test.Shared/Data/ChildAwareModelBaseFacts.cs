// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ChildAwareModelBaseFacts.cs" company="Catel development team">
//   Copyright (c) 2008 - 2017 Catel development team. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------


namespace Catel.Test.Data
{
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows.Data;

    using Catel.Data;
    using Catel.MVVM;
    using Catel.Runtime.Serialization;

    using NUnit.Framework;

    [TestFixture]
    public class ChildAwareModelBaseFacts
    {
        public class ModelWithObservableCollection : ChildAwareModelBase
        {
            /// <summary>
            /// Gets or sets the property value.
            /// </summary>
            public ObservableCollection<int> Collection
            {
                get { return GetValue<ObservableCollection<int>>(CollectionProperty); }
                set { SetValue(CollectionProperty, value); }
            }

            /// <summary>
            /// Register the Collection property so it is known in the class.
            /// </summary>
            public static readonly PropertyData CollectionProperty = RegisterProperty("Collection", typeof(ObservableCollection<int>), () => new ObservableCollection<int>());

            public bool HasCollectionChanged { get; private set; }

            public bool HasPropertyChanged { get; private set; }

            protected override void OnPropertyObjectCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
            {
                HasCollectionChanged = true;
            }

            protected override void OnPropertyObjectPropertyChanged(object sender, PropertyChangedEventArgs e)
            {
                HasPropertyChanged = true;
            }
        }

#if NET
        public class ModelWithCollectionViewSource : ViewModelBase
        {
            /// <summary>
            /// Gets or sets the property value.
            /// </summary>
            public CollectionView Collection
            {
                get { return GetValue<CollectionView>(CollectionProperty); }
                set { SetValue(CollectionProperty, value); }
            }

            /// <summary>
            /// Register the Collection property so it is known in the class.
            /// </summary>
            public static readonly PropertyData CollectionProperty = RegisterProperty("Collection", typeof(CollectionView), () => new CollectionView(new List<int>() { 1, 2, 3 }));
        }
#endif

        [TestCase]
        public void AllowsRegistrationOfObservableCollection()
        {
            var model = new ModelWithObservableCollection();
            model.Collection = new ObservableCollection<int>(new List<int>() { 1, 2, 3 });

            model.Collection.Add(4);

            Assert.IsTrue(model.HasCollectionChanged);
        }

#if NET
        [TestCase]
        public void AllowsRegistrationOfCollectionViewSource()
        {
            var model = new ModelWithCollectionViewSource();
            model.Collection = new CollectionView(new List<int>() { 1, 2, 3 });
        }
#endif

        [TestCase]
        public void RegistersChangeNotificationsOfDefaultValues()
        {
            var model = new ModelWithObservableCollection();

            model.Collection.Add(4);

            Assert.IsTrue(model.HasCollectionChanged);
        }

        //[TestCase]
        //public void IsDirtyWithChildrenWhenSavingChild()
        //{
        //    // Create a collection
        //    var computerSettings = ModelBaseTestHelper.CreateComputerSettingsObject();
        //    computerSettings.SaveObjectToDummyMemoryStream(SerializationFactory.GetXmlSerializer());
        //    Assert.IsFalse(computerSettings.IsDirty);

        //    // Make a chance in the lowest level (but only if ObservableCollection, that is the only supported type)
        //    computerSettings.IniFileCollection[0].FileName = "is dirty should be enabled now";
        //    Assert.IsTrue(computerSettings.IniFileCollection[0].IsDirty);
        //    Assert.IsTrue(computerSettings.IsDirty);

        //    // Save the lowest level (so the parent stays dirty)
        //    computerSettings.IniFileCollection[0].IniEntryCollection[0].SaveObjectToDummyMemoryStream(SerializationFactory.GetXmlSerializer());
        //    Assert.IsFalse(computerSettings.IniFileCollection[0].IniEntryCollection[0].IsDirty);
        //    Assert.IsTrue(computerSettings.IsDirty);
        //}

        //[TestCase]
        //public void IsDirtyWithChildrenWhenSavingParent()
        //{
        //    // Create a collection
        //    var computerSettings = ModelBaseTestHelper.CreateComputerSettingsObject();
        //    computerSettings.SaveObjectToDummyMemoryStream(SerializationFactory.GetXmlSerializer());
        //    Assert.IsFalse(computerSettings.IsDirty);

        //    // Make a chance in the lowest level (but only if ObservableCollection, that is the only supported type)
        //    computerSettings.IniFileCollection[0].FileName = "is dirty should be enabled now 2";
        //    Assert.IsTrue(computerSettings.IniFileCollection[0].IsDirty);
        //    Assert.IsTrue(computerSettings.IsDirty);

        //    // Save the top level
        //    computerSettings.SaveObjectToDummyMemoryStream(SerializationFactory.GetXmlSerializer());
        //    Assert.IsFalse(computerSettings.IniFileCollection[0].IniEntryCollection[0].IsDirty);
        //    Assert.IsFalse(computerSettings.IsDirty);
        //}

        public class ChildModel : ValidatableModelBase
        {
            public static readonly PropertyData NameProperty = RegisterProperty<ChildModel, string>(model => model.Name);

            public string Name
            {
                get => this.GetValue<string>(NameProperty);
                set => this.SetValue(NameProperty, value);
            }

            protected override void ValidateBusinessRules(List<IBusinessRuleValidationResult> validationResults)
            {
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    validationResults.Add(BusinessRuleValidationResult.CreateError("B: There is no name"));
                }
            }

            protected override void ValidateFields(List<IFieldValidationResult> validationResults)
            {
                if (string.IsNullOrWhiteSpace(this.Name))
                {
                    validationResults.Add(FieldValidationResult.CreateError(nameof(NameProperty), "F: There is no name"));
                }
            }
        }

        public class ParentModel : ChildAwareModelBase
        {
            public static readonly PropertyData ChildProperty = RegisterProperty<ParentModel, ChildModel>(model => model.Child);

            public ChildModel Child
            {
                get => this.GetValue<ChildModel>(ChildProperty);
                set => this.SetValue(ChildProperty, value);
            }

            protected override void ValidateBusinessRules(List<IBusinessRuleValidationResult> validationResults)
            {
                if (this.Child != null)
                {
                    var errors = this.Child.GetErrorMessage();
                    if (errors.Length != 0)
                    {
                        validationResults.Add(BusinessRuleValidationResult.CreateError(errors));
                    }
                }
            }

            protected override void ValidateFields(List<IFieldValidationResult> validationResults)
            {
                if (this.Child == null)
                {
                    validationResults.Add(FieldValidationResult.CreateError(nameof(ChildProperty), "F: There is no child"));
                }
            }
        }

        [Test]
        public void ParentGetsValidatedForChildPropertyChange()
        {
            // Setup models
            var c = new ChildModel();
            var p = new ParentModel { Child = c };

            // Create error in child
            c.Name = string.Empty;

            Assert.IsNotEmpty(p.GetErrorMessage());
        }
    }
}