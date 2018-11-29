// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions;
using Microsoft.CodeAnalysis.ExtractMethod;
using Microsoft.CodeAnalysis.Fading;
using Microsoft.CodeAnalysis.ImplementType;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Structure;
using Microsoft.CodeAnalysis.SymbolSearch;
using Microsoft.CodeAnalysis.ValidateFormatString;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using FontsAndColorsCategory = Microsoft.VisualStudio.Shell.Interop.FontsAndColorsCategory;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.Options
{
    internal partial class AdvancedOptionPageControl : AbstractOptionPageControl
    {
        readonly EnhancedColorApplier enhancedColorApplier;

        public AdvancedOptionPageControl(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            InitializeComponent();

            enhancedColorApplier = new EnhancedColorApplier(serviceProvider);

            BindToFullSolutionAnalysisOption(Enable_full_solution_analysis, LanguageNames.CSharp);
            BindToOption(Perform_editor_feature_analysis_in_external_process, RemoteFeatureOptions.OutOfProcessAllowed);
            BindToOption(Enable_navigation_to_decompiled_sources, FeatureOnOffOptions.NavigateToDecompiledSources);

            BindToOption(PlaceSystemNamespaceFirst, GenerationOptions.PlaceSystemNamespaceFirst, LanguageNames.CSharp);
            BindToOption(SeparateImportGroups, GenerationOptions.SeparateImportDirectiveGroups, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInReferenceAssemblies, SymbolSearchOptions.SuggestForTypesInReferenceAssemblies, LanguageNames.CSharp);
            BindToOption(SuggestForTypesInNuGetPackages, SymbolSearchOptions.SuggestForTypesInNuGetPackages, LanguageNames.CSharp);
            BindToOption(Split_string_literals_on_enter, SplitStringLiteralOptions.Enabled, LanguageNames.CSharp);

            BindToOption(EnterOutliningMode, FeatureOnOffOptions.Outlining, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_declaration_level_constructs, BlockStructureOptions.ShowOutliningForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_code_level_constructs, BlockStructureOptions.ShowOutliningForCodeLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_outlining_for_comments_and_preprocessor_regions, BlockStructureOptions.ShowOutliningForCommentsAndPreprocessorRegions, LanguageNames.CSharp);
            BindToOption(Collapse_regions_when_collapsing_to_definitions, BlockStructureOptions.CollapseRegionsWhenCollapsingToDefinitions, LanguageNames.CSharp);

            BindToOption(Fade_out_unused_usings, FadingOptions.FadeOutUnusedImports, LanguageNames.CSharp);
            BindToOption(Fade_out_unreachable_code, FadingOptions.FadeOutUnreachableCode, LanguageNames.CSharp);

            BindToOption(Show_guides_for_declaration_level_constructs, BlockStructureOptions.ShowBlockStructureGuidesForDeclarationLevelConstructs, LanguageNames.CSharp);
            BindToOption(Show_guides_for_code_level_constructs, BlockStructureOptions.ShowBlockStructureGuidesForCodeLevelConstructs, LanguageNames.CSharp);

            BindToOption(GenerateXmlDocCommentsForTripleSlash, FeatureOnOffOptions.AutoXmlDocCommentGeneration, LanguageNames.CSharp);
            BindToOption(InsertAsteriskAtTheStartOfNewLinesWhenWritingBlockComments, FeatureOnOffOptions.AutoInsertBlockCommentStartString, LanguageNames.CSharp);
            BindToOption(DisplayLineSeparators, FeatureOnOffOptions.LineSeparator, LanguageNames.CSharp);
            BindToOption(EnableHighlightReferences, FeatureOnOffOptions.ReferenceHighlighting, LanguageNames.CSharp);
            BindToOption(EnableHighlightKeywords, FeatureOnOffOptions.KeywordHighlighting, LanguageNames.CSharp);
            BindToOption(RenameTrackingPreview, FeatureOnOffOptions.RenameTrackingPreview, LanguageNames.CSharp);

            BindToOption(DontPutOutOrRefOnStruct, ExtractMethodOptions.DontPutOutOrRefOnStruct, LanguageNames.CSharp);
            BindToOption(AllowMovingDeclaration, ExtractMethodOptions.AllowMovingDeclaration, LanguageNames.CSharp);

            BindToOption(with_other_members_of_the_same_kind, ImplementTypeOptions.InsertionBehavior, ImplementTypeInsertionBehavior.WithOtherMembersOfTheSameKind, LanguageNames.CSharp);
            BindToOption(at_the_end, ImplementTypeOptions.InsertionBehavior, ImplementTypeInsertionBehavior.AtTheEnd, LanguageNames.CSharp);

            BindToOption(prefer_throwing_properties, ImplementTypeOptions.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferThrowingProperties, LanguageNames.CSharp);
            BindToOption(prefer_auto_properties, ImplementTypeOptions.PropertyGenerationBehavior, ImplementTypePropertyGenerationBehavior.PreferAutoProperties, LanguageNames.CSharp);

            BindToOption(Report_invalid_placeholders_in_string_dot_format_calls, ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp);

            BindToOption(Colorize_regular_expressions, RegularExpressionsOptions.ColorizeRegexPatterns, LanguageNames.CSharp);
            BindToOption(Report_invalid_regular_expressions, RegularExpressionsOptions.ReportInvalidRegexPatterns, LanguageNames.CSharp);
            BindToOption(Highlight_related_components_under_cursor, RegularExpressionsOptions.HighlightRelatedRegexComponentsUnderCursor, LanguageNames.CSharp);
        }

        private void Apply_enhanced_colors_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            enhancedColorApplier.SetEnhancedColors();
        }

        private void Apply_classic_colors_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            enhancedColorApplier.SetDefaultColors();
        }

        class EnhancedColorApplier
        {
            readonly IVsFontAndColorDefaultsProvider fontAndColorDefaultsProvider;
            readonly IVsFontAndColorCacheManager fontAndColorManager;
            readonly IVsFontAndColorStorage fontAndColorStorage;
            readonly IVsFontAndColorStorage3 fontAndColorStorage3;
            readonly IVsFontAndColorUtilities fontAndColorUtilities;
            readonly DTE dte;
            readonly Guid editorColorsGuid;

            const uint DefaultForegroundColor = 0x01000000u;
            const uint DefaultBackgroundColor = 0x01000001u;

            public EnhancedColorApplier(IServiceProvider serviceProvider)
            {
                fontAndColorDefaultsProvider = (IVsFontAndColorDefaultsProvider)serviceProvider.GetService(typeof(IVsFontAndColorDefaultsProvider));
                fontAndColorManager = (IVsFontAndColorCacheManager)serviceProvider.GetService(typeof(IVsFontAndColorCacheManager));
                fontAndColorStorage = (IVsFontAndColorStorage)serviceProvider.GetService(typeof(IVsFontAndColorStorage));
                fontAndColorStorage3 = (IVsFontAndColorStorage3)serviceProvider.GetService(typeof(IVsFontAndColorStorage3));
                fontAndColorUtilities = (IVsFontAndColorUtilities)serviceProvider.GetService(typeof(IVsFontAndColorUtilities));
                dte = (DTE)serviceProvider.GetService(typeof(DTE));

                editorColorsGuid = new Guid(FontsAndColorsCategory.TextEditor);
            }

            public void SetDefaultColors()
            { 
                var props = dte.Properties["FontsAndColors", "TextEditor"];
                var prop = props.Item("FontsAndColorsItems");

                var colorItemMap = CreateColorableItemsMap((FontsAndColorsItems)prop.Object);

                colorItemMap["Identifier"].Foreground = DefaultForegroundColor;
                colorItemMap["label name"].Foreground = DefaultForegroundColor;
                colorItemMap["namespace name"].Foreground = DefaultForegroundColor;
                colorItemMap["method name"].Foreground = DefaultForegroundColor;
                colorItemMap["extension method name"].Foreground = DefaultForegroundColor;
                colorItemMap["keyword - control"].Foreground = DefaultForegroundColor;
                colorItemMap["operator - overload"].Bold = false;
                colorItemMap["static symbol"].Bold = false;
            }

            public void SetEnhancedColors()
            {
                var props = dte.Properties["FontsAndColors", "TextEditor"];
                var prop = props.Item("FontsAndColorsItems");

                var colorItemMap = CreateColorableItemsMap((FontsAndColorsItems)prop.Object);

                if (IsDarkTheme())
                {
                    // Dark Theme
                    colorItemMap["Identifier"].Foreground = 0x00FEDC9Cu;
                    colorItemMap["label name"].Foreground = 0x00FFFFFFu;
                    colorItemMap["namespace name"].Foreground = 0x00B0C94Eu;
                    colorItemMap["method name"].Foreground = 0x00AADCDCu;
                    colorItemMap["keyword - control"].Foreground = 0x00E694EEu;
                    colorItemMap["operator - overload"].Bold = true;
                    colorItemMap["static symbol"].Bold = true;
                }
                else
                {
                    // Light or Blue themes
                    colorItemMap["Identifier"].Foreground = 0x00801000u;
                    colorItemMap["label name"].Foreground = 0x00000000u;
                    colorItemMap["namespace name"].Foreground = 0x00AF912Bu;
                    colorItemMap["method name"].Foreground = 0x001F5374u;
                    colorItemMap["keyword - control"].Foreground = 0x00C4088Fu;
                    colorItemMap["operator - overload"].Bold = true;
                    colorItemMap["static symbol"].Bold = true;
                }
            }

            private Dictionary<string, ColorableItems> CreateColorableItemsMap(FontsAndColorsItems fontsAndColorsItems)
            {
                return Enumerable.Range(1, fontsAndColorsItems.Count)
                    .Select(index => fontsAndColorsItems.Item(index))
                    .ToDictionary(item => item.Name);
            }

            private bool IsDarkTheme()
            {
                const string DarkThemeGuid = "1ded0138-47ce-435e-84ef-9ec1f439b749";
                return GetThemeId() == DarkThemeGuid;
            }

            public string GetThemeId()
            {
                try
                {
                    var currentTheme = dte.Properties["Environment", "General"].Item("SelectedTheme").Value;
                    var themeId = currentTheme.GetType().GetProperty("ThemeId").GetValue(currentTheme);
                    return themeId.ToString();
                }
                catch
                {
                    var keyName = $@"Software\Microsoft\VisualStudio\{dte.Version}\ApplicationPrivateSettings\Microsoft\VisualStudio";
                    using (var key = Registry.CurrentUser.OpenSubKey(keyName))
                    {
                        var keyText = key?.GetValue("ColorTheme", string.Empty) as string;
                        if (string.IsNullOrEmpty(keyText))
                        {
                            return null;
                        }

                        var keyTextValues = keyText.Split('*');
                        if (keyTextValues.Length < 3)
                        {
                            return null;
                        }

                        return keyTextValues[2];
                    }
                }
            }
        }
    }
}
