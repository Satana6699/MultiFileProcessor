using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using Microsoft.Office.Interop.Word;
using Window = System.Windows.Window;
using Application = Microsoft.Office.Interop.Word.Application;

namespace FileAggregator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnExecuteButtonClick(object sender, RoutedEventArgs e)
        {
            string sourcePath = SourcePathTextBox.Text;
            string outputFilePath = OutputFileTextBox.Text;

            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(outputFilePath))
            {
                LogMessage("Укажите оба пути: исходный и выходной.");
                return;
            }

            if (!Directory.Exists(sourcePath))
            {
                LogMessage("Исходная папка не найдена.");
                return;
            }

            var extensions = ExtensionsPanel.Children.OfType<StackPanel>()
                               .Select(sp => sp.Children.OfType<TextBox>().FirstOrDefault()?.Text)
                               .Where(ext => !string.IsNullOrWhiteSpace(ext) && ext.StartsWith("."))
                               .ToList();

            if (extensions.Count == 0)
            {
                LogMessage("Добавьте хотя бы одно расширение.");
                return;
            }

            try
            {
                var files = Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
                                     .Where(file => extensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                                     .ToList();

                GenerateWordDocument(files, outputFilePath, TwoColumnsCheckBox.IsChecked == true);
                LogMessage($"Объединение завершено. Сохранено в {outputFilePath}");
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка: {ex.Message}");
            }
        }

        private void GenerateWordDocument(List<string> files, string outputFilePath, bool twoColumns)
        {
            Application wordApp = new Application();
            Document doc = wordApp.Documents.Add();

            if (twoColumns)
            {
                doc.PageSetup.TextColumns.SetCount(2);
            }

            foreach (var file in files)
            {
                // Добавляем имя файла с форматированием
                Paragraph fileNamePara = doc.Content.Paragraphs.Add();
                fileNamePara.Range.Text = $"\n{Path.GetFileName(file)}\n";
                fileNamePara.Range.Font.Name = "Times New Roman";
                fileNamePara.Range.Font.Size = 14;
                fileNamePara.Range.Font.Bold = 1;
                fileNamePara.Format.SpaceAfter = 12;
                fileNamePara.Range.InsertParagraphAfter();

                // Добавляем содержимое файла
                Paragraph contentPara = doc.Content.Paragraphs.Add();
                contentPara.Range.Text = File.ReadAllText(file);
                contentPara.Range.Font.Name = "Times New Roman";
                contentPara.Range.Font.Size = 10;
                contentPara.Range.Font.Bold = 0;
                contentPara.Range.Font.Color = WdColor.wdColorBlack;
                contentPara.Range.InsertParagraphAfter();
            }

            doc.SaveAs2(outputFilePath);
            doc.Close();
            wordApp.Quit();
        }

        private void OnSelectSourcePathClick(object sender, RoutedEventArgs e)
        {
            var dialog = new VistaFolderBrowserDialog
            {
                Description = "Выберите исходную папку",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
            {
                SourcePathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void OnSelectOutputFileClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Word Documents (*.docx)|*.docx|All Files (*.*)|*.*",
                DefaultExt = "docx",
                Title = "Выберите файл для сохранения"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                OutputFileTextBox.Text = saveFileDialog.FileName;
            }
        }

        private void OnAddExtensionClick(object sender, RoutedEventArgs e)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

            var textBox = new TextBox { Width = 250, Margin = new Thickness(0, 0, 5, 0) };
            var removeButton = new Button { Content = "Удалить", Margin = new Thickness(5, 0, 0, 0) };
            removeButton.Click += OnRemoveExtensionClick;

            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(removeButton);

            ExtensionsPanel.Children.Add(stackPanel);
        }

        private void OnRemoveExtensionClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Parent is StackPanel stackPanel)
            {
                ExtensionsPanel.Children.Remove(stackPanel);
            }
        }

        private void LogMessage(string message)
        {
            LogTextBox.AppendText(message + Environment.NewLine);
            LogTextBox.ScrollToEnd();
        }
    }
}