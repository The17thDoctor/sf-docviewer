using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.IO;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows.Media;

namespace Starfall_Documentation_Viewer
{
	public class DocumentationCreator
	{
		private readonly string DocumentationURL = "http://51.68.206.223/sfdoc/docs.json";
		private readonly string DocumentationPath = Path.GetFullPath("resources/documentation.json");
		public string DocumentationJSON = String.Empty;
		private readonly bool LoadLocal = false;

		public JToken? JDocumentation;

		private readonly Dictionary<string, string> OperatorNames = new()
		{
			{ "eq", "{0} == {1}" },
			{ "add", "{0} + {1}" },
			{ "sub", "{0} - {1}" },
			{ "mul", "{0} * {1}" },
			{ "div", "{0} / {1}" },
			{ "pow", "{0} ^ {1}" },
			{ "unm", "-{0}" },
		};

		private string OperatorName(string name)
		{
			string[] SplitStr = name.Split("_");
			return String.Format(OperatorNames[SplitStr[0]], SplitStr[1], SplitStr[2]);
		}

		public static Image GetImage(string ImageName, int size = 16)
		{
			BitmapImage BMP = new();
			BMP.BeginInit();
			BMP.UriSource = new Uri($"resources/images/{ImageName}.png", UriKind.Relative);
			BMP.EndInit();

			Image image = new()
			{
				Width = size,
				Height = size,
				Source = BMP,
			};

			return image;
		}
		public static TreeViewItem CreateTVI(string name, Image icon, string path = "")
		{
			TreeViewItem item = new()
			{
				Tag = path
			};

			StackPanel panel = new()
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Orientation = Orientation.Horizontal,
				Tag = path,
			};

			panel.Children.Add(icon);
			panel.Children.Add(new Label() { Content = name, FontSize = 11 });

			item.Header = panel;
			return item;
		}

		public bool FetchDocumentation()
		{
			if (!LoadLocal)
			{
				try
				{
					Trace.WriteLine("Fetching HTTP Document...");
					HttpClient client = new();
					HttpResponseMessage response = client.GetAsync(DocumentationURL).Result;
					DocumentationJSON = response.Content.ReadAsStringAsync().Result;
					File.WriteAllText(DocumentationPath, DocumentationJSON);
					Trace.WriteLine("Done !");
				}
				catch (Exception)
				{
					Trace.WriteLine("Falling back to stored JSON.");
					DocumentationJSON = File.ReadAllText(DocumentationPath);
					return false;
				}
			} else
			{
				DocumentationJSON = File.ReadAllText(DocumentationPath);
			}

			JDocumentation = (JToken)JsonConvert.DeserializeObject(DocumentationJSON);

			return true;
		}
		public void PopulateTree(TreeView TView)
		{
			JToken Hooks = JDocumentation.SelectToken("hooks");
			JToken Libraries = JDocumentation.SelectToken("libraries");
			JToken Classes = JDocumentation.SelectToken("classes");
			JToken Tables = JDocumentation.SelectToken("tables");
			JToken Directives = JDocumentation.SelectToken("directives");

			/*Hooks*/ {
				TreeViewItem HooksItem = CreateTVI("Hooks", GetImage("hooks"), "hooks");
				TView.Items.Add(HooksItem);

				foreach (JProperty Hook in Hooks)
				{
					if (Int32.TryParse(Hook.Name, out _))
					{
						JToken HookValues = Hooks.SelectToken((string)Hook.Value);
						string SideString = "shared";
						SideString = HookValues.SelectToken("client") != null ? "client" : SideString;
						SideString = HookValues.SelectToken("server") != null ? "server" : SideString;

						TreeViewItem HookItem = CreateTVI((string)Hook.Value, GetImage(SideString), $"hooks.{Hook.Value}");
						HooksItem.Items.Add(HookItem);
					}
				}
			}

			/*Libraries*/ {
				TreeViewItem LibrariesItem = CreateTVI("Libraries", GetImage("libraries"), "libraries");
				TView.Items.Add(LibrariesItem);

				foreach (JProperty Library in Libraries)
				{
					if (Int32.TryParse(Library.Name, out _))
					{
						TreeViewItem LibraryItem = CreateTVI((string)Library.Value, GetImage("library"), Library.Path);
						LibrariesItem.Items.Add(LibraryItem);

						string LibraryName = (string)Library.Value;
						LibraryName = JDocumentation.SelectToken($"libraries.{Library.Value}.docname") != null 
							? (string)((JValue)JDocumentation.SelectToken($"libraries.{Library.Value}.docname")).Value
							: LibraryName;

						LibraryName = LibraryName.Replace("_", "__");

						JToken Functions = JDocumentation.SelectToken($"libraries.{Library.Value}.functions");
						if (Functions.Any())
						{
							TreeViewItem FunctionsItem = CreateTVI("Functions", GetImage("functions"), Functions.Path);
							LibraryItem.Items.Add(FunctionsItem);

							foreach (JProperty Function in Functions)
							{
								if (Int32.TryParse(Function.Name, out _))
								{
									JToken Params = JDocumentation.SelectToken($"{Functions.Path}.{Function.Value}");
									string SideString = "shared";
									SideString = Params.SelectToken("client") != null ? "client" : SideString;
									SideString = Params.SelectToken("server") != null ? "server" : SideString;

									TreeViewItem FunctionItem = CreateTVI($"{LibraryName}.{Function.Value}()", GetImage(SideString), $"{Functions.Path}.{(string)Function.Value}");
									FunctionsItem.Items.Add(FunctionItem);
								}
							}
						}

						JToken Fields = JDocumentation.SelectToken($"libraries.{Library.Value}.fields");
						if (Fields != null)
						{
							TreeViewItem FieldsItem = CreateTVI("Fields", GetImage("classes"), Fields.Path);
							LibraryItem.Items.Add(FieldsItem);

							foreach (JProperty Field in Fields)
							{
								if (Int32.TryParse(Field.Name, out _))
								{
									JToken Params = JDocumentation.SelectToken($"{Fields.Path}.{Field.Value}");
									string SideString = "shared";
									SideString = Params.SelectToken("client") != null ? "client" : SideString;
									SideString = Params.SelectToken("server") != null ? "server" : SideString;

									TreeViewItem FieldItem = CreateTVI($"{LibraryName}.{Field.Value}", GetImage(SideString), $"{Fields.Path}.{(string)Field.Value}");
									FieldsItem.Items.Add(FieldItem);
								}
							}
						}

						/*Tables*/ {
							JToken LTables = JDocumentation.SelectToken($"libraries.{Library.Value}.tables");
							if (LTables != null)
							{
								TreeViewItem TablesItem = CreateTVI("Tables", GetImage("library_tables"), LTables.Path);
								LibraryItem.Items.Add(TablesItem);

								foreach (JProperty Table in LTables)
								{
									if (Int32.TryParse(Table.Name, out _))
									{
										JToken Params = JDocumentation.SelectToken($"{LTables.Path}.{Table.Value}");
										string SideString = "shared";
										SideString = Params.SelectToken("client") != null ? "client" : SideString;
										SideString = Params.SelectToken("server") != null ? "server" : SideString;

										TreeViewItem TableItem = CreateTVI($"{LibraryName}.{Table.Value}", GetImage(SideString), $"{LTables.Path}.{(string)Table.Value}");
										TablesItem.Items.Add(TableItem);
									}
								}
							}
						}
					}
				}
			}

			/*Classes*/ {
				TreeViewItem ClassesItem = CreateTVI("Types", GetImage("classes"), "classes");
				TView.Items.Add(ClassesItem);

				foreach (JProperty Class in Classes)
				{
					if (Int32.TryParse(Class.Name, out _))
					{
						TreeViewItem ClassItem = CreateTVI((string)Class.Value, GetImage("directive"), $"classes.{Class.Value}");
						ClassesItem.Items.Add(ClassItem);

						JToken Methods = JDocumentation.SelectToken($"classes.{Class.Value}.methods");
						JToken Operators = JDocumentation.SelectToken($"classes.{Class.Value}.operators");

						bool IgnoreMainLabel = false;
						if (Methods == null || Operators == null) IgnoreMainLabel = true;

						if (Methods != null)
						{
							TreeViewItem Item;
							if (IgnoreMainLabel) Item = ClassItem;
							else
							{
								Item = CreateTVI("Methods", GetImage("methods"), $"{Methods.Path}");
								ClassItem.Items.Add(Item);
							}
							foreach (JProperty Method in Methods)
							{
								if (Int32.TryParse(Method.Name, out _))
								{
									JToken Params = JDocumentation.SelectToken($"{Methods.Path}.{Method.Value}");
									string SideString = "shared";
									SideString = Params.SelectToken("client") != null && Params.SelectToken("server") == null ? "client" : SideString;
									SideString = Params.SelectToken("server") != null && Params.SelectToken("client") == null ? "server" : SideString;

									TreeViewItem MethodItem = CreateTVI($"{Class.Value}:{Method.Value}()", GetImage(SideString), $"{Methods.Path}.{Method.Value}");
									Item.Items.Add(MethodItem);
								}
							}
						}

						if (Operators != null)
						{
							TreeViewItem Item;
							if (IgnoreMainLabel) Item = ClassItem;
							else
							{
								Item = CreateTVI("Operators", GetImage("operators"), $"{Operators.Path}");
								ClassItem.Items.Add(Item);
							}
							foreach (JProperty Operator in Operators)
							{
								if (Int32.TryParse(Operator.Name, out _))
								{
									TreeViewItem OperatorItem = CreateTVI(OperatorName((string)Operator.Value), GetImage("directive"), $"{Operators.Path}.{Operator.Value}");
									Item.Items.Add(OperatorItem);
								}
							}
						}
					}

				}
			}

			/*Tables*/ {
				TreeViewItem TablesItem = CreateTVI("Structures", GetImage("tables"), "tables");
				TView.Items.Add(TablesItem);

				foreach (JProperty Table in Tables)
				{
					if (Int32.TryParse(Table.Name, out _))
					{
						TreeViewItem TableItem = CreateTVI((string)Table.Value, GetImage("table"), $"tables.{Table.Value}");
						TablesItem.Items.Add(TableItem);
						JToken Fields = JDocumentation.SelectToken($"tables.{Table.Value}.field");

						foreach (JProperty Field in Fields)
						{
							if (Int32.TryParse(Field.Name, out _))
							{
								JToken Params = JDocumentation.SelectToken($"{Fields.Path}.{Field.Value}");
								string SideString = "shared";
								SideString = Params.SelectToken("client") != null ? "client" : SideString;
								SideString = Params.SelectToken("server") != null ? "server" : SideString;

								TreeViewItem FieldItem = CreateTVI($"{Table.Value}.{Field.Value}", GetImage(SideString), $"{Fields.Path}.{Field.Value}");
								TableItem.Items.Add(FieldItem);
							}
						}
					}
				}
			}

			/*Directives*/ {
				TreeViewItem DirectivesItem = CreateTVI("Preprocessor Directives", GetImage("directives"), "directives");
				TView.Items.Add(DirectivesItem);

				foreach (JProperty Directive in Directives)
				{
					if (Int32.TryParse(Directive.Name, out _))
					{
						TreeViewItem DirectiveItem = CreateTVI((string)Directive.Value, GetImage("directive"), $"directives.{Directive.Value}");
						DirectivesItem.Items.Add(DirectiveItem);
					}
				}
			}
		}

		public static Label CreateExampleLabel(string Example, bool Deprecated = false)
        {
			Label ExLabel = new()
			{
				Background = new SolidColorBrush() { Color = Deprecated ? Color.FromRgb(255, 201, 201) : Color.FromRgb(255, 255, 201) },
				Foreground = new SolidColorBrush() { Color = Color.FromRgb(0, 0, 0) },
				Content = Example,
				Margin = new Thickness(15, 10, 15, 0),
				FontSize = 17,
				Height = 40,
				FontWeight = FontWeights.SemiBold,
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Stretch,
			};

			return ExLabel;
		}

		public static TextBlock CreateDescriptionLabel(string Description)
        {
			TextBlock DescBlock = new()
			{
				Text = Description,
				Margin = new Thickness(25, 15, 25, 0),
				FontWeight = FontWeights.SemiBold,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Top,
				TextWrapping = TextWrapping.Wrap,
				Foreground = new SolidColorBrush() { Color = Colors.White },
				FontSize = 14,
				LineHeight = 20,
            };

			return DescBlock;
        }

		public static TextBlock CreateParamsLabel(string Parameters)
		{
			TextBlock DescBlock = new()
			{
				Text = Parameters,
				Margin = new Thickness(25, 15, 25, 0),
				FontWeight = FontWeights.SemiBold,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Top,
				TextWrapping = TextWrapping.Wrap,
				Foreground = new SolidColorBrush() { Color = Colors.White },
				FontSize = 13,
				LineHeight = 20,
			};

			return DescBlock;
		}

		public static Label CreateNoteLabel(string Type, string Value)
		{
			Label NoteLabel = new()
			{
				FontSize = 15,
				FontWeight = FontWeights.Bold,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Top,
				Margin = new Thickness(20, 15, 0, 0),
				VerticalContentAlignment= VerticalAlignment.Center
            };

			if (Type == "side")
            {
				if (Value == "shared") NoteLabel.Content = "Can be used on both sides.";
				else if (Value == "client") NoteLabel.Content = "Can only be used clientside.";
				else NoteLabel.Content = "Can only be used serverside.";
			} else
            {

            }

			return NoteLabel;
		}

		public void CreatePage(string Path, StackPanel Page)
        {
			string[] SplitPath = Path.Split('.');
			Page.Children.Clear();
			if (SplitPath.Length < 2) return;

			switch (SplitPath[0])
            {
				case "hooks":
					CreateHookPage(Path, Page);
					break;
				case "libraries":
					CreateLibraryPage(Path, Page);
					break;
				case "classes":
					CreateClassPage(Path, Page);
					break;
				case "tables":
					CreateTablePage(Path, Page);
					break;
				case "directives":
					CreateDirectivePage(Path, Page);
					break;
				default: return;
            }
		}

		public void CreateHookPage(string Path, StackPanel Page)
        {
			string[] SplitPath = Path.Split('.');
			string HookType = SplitPath[1];
			JToken HookInfos = JDocumentation.SelectToken(Path);
			List<String> HookParams = new() { };
			JToken Params = HookInfos.SelectToken("param");
			string SideString = "shared";
			SideString = HookInfos.SelectToken("server") != null ? "server" : SideString;
			SideString = HookInfos.SelectToken("client") != null ? "client" : SideString;

			Page.Children.Add(new Label() { Content = "Hook - " + HookType, FontSize = 18, Margin = new Thickness(15, 15, 0, 0) });
			Page.Children.Add(CreateNoteLabel("side", SideString));
			Page.Children.Add(CreateDescriptionLabel((string)HookInfos.SelectToken("description")));
			
			if (Params != null)
            {
				string ParamsString = "Parameters :";
				foreach (JProperty Param in Params)
				{
					if (Int32.TryParse(Param.Name, out _))
                    {
						HookParams.Add((string)Param.Value);
						string ParamDesc = (string)Params.SelectToken((string)Param.Value);
						ParamsString += $"\n   {Param.Value}.   {ParamDesc}";
                    }
				}

				Page.Children.Add(CreateParamsLabel(ParamsString));
			}

			string ExampleString = $"hook( \"{HookType}\", \"name\", function({String.Join(", ", HookParams)}) end)";
			Page.Children.Insert(1, CreateExampleLabel(ExampleString, HookInfos.SelectToken("deprecated") != null));
        }

		public void CreateLibraryPage(string Path, StackPanel Page)
        {

        }

		public void CreateClassPage(string Path, StackPanel Page)
		{

		}

		public void CreateTablePage(string Path, StackPanel Page)
		{

		}

		public void CreateDirectivePage(string Path, StackPanel Page)
		{
			string[] SplitPath = Path.Split('.');
			string DirectiveName = SplitPath[1];
			JToken DirectiveInfos = JDocumentation.SelectToken(Path);
			JToken Params = DirectiveInfos.SelectToken("param");
			
			Page.Children.Add(CreateDescriptionLabel((string)DirectiveInfos.SelectToken("description")));

			if (Params != null)
            {
				string ParamsString = "Parameters :";
				foreach (JProperty Param in Params)
                {
					if (Int32.TryParse(Param.Name, out _))
                    {
						DirectiveName += $" {Param.Value}";
						ParamsString += $"\n   {Param.Value}.   {Params.SelectToken((string)Param.Value)}";

					}
                }

				Page.Children.Add(CreateParamsLabel(ParamsString));
            }

			Page.Children.Insert(0, CreateExampleLabel($"--@{DirectiveName}"));
		}
	}
}
