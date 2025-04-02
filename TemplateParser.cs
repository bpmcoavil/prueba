using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using TemplateParser.Modificators;

namespace TemplateParser
{

    public class Parser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string _strTemplateBlock;

        private Hashtable _hstValues;

        private Hashtable _ErrorMessage = new Hashtable();

        private string _ParsedBlock;

        private string _strSubTemplatePath = string.Empty;

        private Dictionary<string, Parser> _Blocks = new Dictionary<string, Parser>();

        private string VariableTagBegin = "##";

        private string VariableTagEnd = "##";

        private string SubTemplateTagBegin = "##SubTemplate--";

        private string SubTemplateTagEnd = "##";

        private string ModificatorTag = ":";

        private string ModificatorParamSep = ",";

        private string ModificatorInstance = "INSTANCE";

        private string ModificatorTagVar = "$$";

        private string _strInstanceVariable = "SOL_INSTANCIA";

        private string ConditionTagIfBegin = "##If{0}--";

        private string ConditionTagIfEnd = "##";

        private string ConditionTagElseBegin = "##Else{0}--";

        private string ConditionTagElseEnd = "##";

        private string ConditionTagEndIfBegin = "##EndIf{0}--";

        private string ConditionTagEndIfEnd = "##";

        private string BlockTagBeginBegin = "##BlockBegin--";

        private string BlockTagBeginEnd = "##";

        private string BlockTagEndBegin = "##BlockEnd--";

        private string BlockTagEndEnd = "##";

        public string TemplateBlock
        {
            get
            {
                return _strTemplateBlock;
            }
            set
            {
                _strTemplateBlock = value;
                ParseBlocks();
            }
        }

        public Hashtable Variables
        {
            get
            {
                return _hstValues;
            }
            set
            {
                _hstValues = value;
            }
        }

        public Hashtable ErrorMessage => _ErrorMessage;

        public Dictionary<string, Parser> Blocks => _Blocks;

        public string SubTemplatePath
        {
            get
            {
                return _strSubTemplatePath;
            }
            set
            {
                _strSubTemplatePath = value;
            }
        }

        public string InstanceVariable
        {
            get
            {
                return _strInstanceVariable;
            }
            set
            {
                _strInstanceVariable = value;
            }
        }

        public Parser()
        {
            _strTemplateBlock = "";
        }

        public Parser(string FilePath)
        {
            ReadTemplateFromFile(FilePath);
            ParseBlocks();
        }

        public Parser(Hashtable Variables)
        {
            _hstValues = Variables;
        }

        public Parser(string FilePath, Hashtable Variables)
        {
            ReadTemplateFromFile(FilePath);
            _hstValues = Variables;
            ParseBlocks();
        }

        public void SetTemplateFromFile(string FilePath)
        {
            ReadTemplateFromFile(FilePath);
        }

        public void SetTemplate(string TemplateBlock)
        {
            this.TemplateBlock = TemplateBlock;
        }

        public string Parse()
        {
            return Parse(1);
        }

        public string Parse(int maxLevel)
        {
            for (int num = maxLevel; num > 0; num--)
            {
                _strTemplateBlock = ParseConditions(_strTemplateBlock, num);
            }

            if (SubTemplatePath != string.Empty)
            {
                _strTemplateBlock = ParseSubtemplates(_strTemplateBlock);
            }

            _strTemplateBlock = ParseVariables(_strTemplateBlock);
            return _strTemplateBlock;
        }

        public string ParseBlock(string BlockName, Hashtable Variables)
        {
            if (!_Blocks.ContainsKey(BlockName))
            {
                throw new ArgumentException($"Could not find Block with Name '{BlockName}'");
            }

            _Blocks[BlockName].Variables = Variables;
            string templateBlock = _Blocks[BlockName].TemplateBlock;
            string result = _Blocks[BlockName].Parse();
            _Blocks[BlockName].TemplateBlock = templateBlock;
            return result;
        }

        public bool ParseToFile(string FilePath, bool ReplaceIfExists)
        {
            if (File.Exists(FilePath) && !ReplaceIfExists)
            {
                return false;
            }

            StreamWriter streamWriter = File.CreateText(FilePath);
            streamWriter.Write(Parse());
            streamWriter.Close();
            return true;
        }

        private void ReadTemplateFromFile(string FilePath)
        {
            if (!File.Exists(FilePath))
            {
                throw new ArgumentException("Template file does not exist." + FilePath);
            }

            StreamReader streamReader = new StreamReader(FilePath);
            TemplateBlock = streamReader.ReadToEnd();
            streamReader.Close();
        }

        private void ParseBlocks()
        {
            int startIndex = 0;
            while ((startIndex = _strTemplateBlock.IndexOf(BlockTagBeginBegin, startIndex)) != -1)
            {
                int num = startIndex;
                startIndex += BlockTagBeginBegin.Length;
                int num2 = _strTemplateBlock.IndexOf(BlockTagBeginEnd, startIndex);
                if (num2 == -1)
                {
                    throw new Exception("Could not find BlockTagBeginEnd");
                }

                string text = _strTemplateBlock.Substring(startIndex, num2 - startIndex);
                startIndex = num2 + BlockTagBeginEnd.Length;
                string value = BlockTagEndBegin + text + BlockTagEndEnd;
                int num3 = _strTemplateBlock.IndexOf(value, startIndex);
                if (num3 == -1)
                {
                    throw new Exception("Could not find End of Block with name '" + text + "'");
                }

                Parser parser = new Parser();
                parser.TemplateBlock = _strTemplateBlock.Substring(startIndex, num3 - startIndex);
                _Blocks.Add(text, parser);
                _strTemplateBlock = _strTemplateBlock.Remove(num, num3 - num);
                startIndex = num;
            }
        }

        private string ParseConditions(string templateBlock, int level)
        {
            string arg = level.ToString();
            if (level == 1)
            {
                arg = "";
            }

            string text = string.Format(ConditionTagIfBegin, arg);
            string conditionTagIfEnd = ConditionTagIfEnd;
            string text2 = string.Format(ConditionTagElseBegin, arg);
            string conditionTagElseEnd = ConditionTagElseEnd;
            string text3 = string.Format(ConditionTagEndIfBegin, arg);
            string conditionTagEndIfEnd = ConditionTagEndIfEnd;
            int num = 0;
            int num2 = 0;
            string text4 = "";
            try
            {
                while ((num2 = templateBlock.IndexOf(text, num2)) != -1)
                {
                    bool flag = false;
                    int num3 = num2;
                    num2 += text.Length;
                    int num4 = templateBlock.IndexOf(conditionTagIfEnd, num2);
                    if (num4 == -1)
                    {
                        throw new Exception("Could not find ConditionTagIfEnd");
                    }

                    string text5 = templateBlock.Substring(num2, num4 - num2);
                    num2 = num4 + conditionTagIfEnd.Length;
                    string text6 = text2 + text5 + conditionTagElseEnd;
                    string text7 = text3 + text5 + conditionTagEndIfEnd;
                    int num5 = templateBlock.IndexOf(text6, num2);
                    int num6 = templateBlock.IndexOf(text7, num2);
                    if (num5 > num6)
                    {
                        int num7 = templateBlock.IndexOf(text, num6);
                        if (num7 < num6)
                        {
                            throw new Exception("Condition Else Tag placed after Condition Tag EndIf for '" + text5 + "'");
                        }

                        num5 = -1;
                    }

                    string text8;
                    string text9;
                    if (num5 != -1)
                    {
                        text8 = templateBlock.Substring(num2, num5 - num2);
                        text9 = templateBlock.Substring(num5 + text6.Length, num6 - num5 - text6.Length);
                    }
                    else
                    {
                        text8 = templateBlock.Substring(num2, num6 - num2);
                        text9 = "";
                    }

                    try
                    {
                        char[] anyOf = new char[5] { '=', '>', '<', '!', '|' };
                        int num8 = text5.IndexOfAny(anyOf);
                        if (num8 >= 0)
                        {
                            string text10 = text5.Substring(num8, 1);
                            string[] array = text5.Split(text10.ToCharArray());
                            text5 = array[0];
                            if (text5.IndexOf(ModificatorInstance) >= 0)
                            {
                                string[] array2 = text5.Split(ModificatorTag.ToCharArray());
                                text5 = array2[0];
                                text5 = text5 + "_" + _hstValues[InstanceVariable].ToString();
                            }

                            string text11 = (string)_hstValues[text5];
                            text11 = text11.ToUpper();
                            string text12 = array[1];
                            if (text12.StartsWith(ModificatorTagVar))
                            {
                                text12 = (string)_hstValues[text12.Replace(ModificatorTagVar, "")];
                            }

                            text12 = text12.ToUpper();
                            double result = 0.0;
                            switch (text10)
                            {
                                case "=":
                                    flag = text11 == text12;
                                    break;
                                case "!":
                                    flag = (text12 != "" && !_hstValues.ContainsKey(text5)) || text11 != text12;
                                    break;
                                case ">":
                                    if (double.TryParse(text11, out result))
                                    {
                                        flag = result > double.Parse(text12);
                                    }

                                    break;
                                case "<":
                                    if (double.TryParse(text11, out result))
                                    {
                                        flag = result < double.Parse(text12);
                                    }

                                    break;
                                case "|":
                                    flag = _hstValues.ContainsKey(text5);
                                    break;
                            }
                        }
                        else
                        {
                            flag = Convert.ToBoolean(_hstValues[text5]);
                        }
                    }
                    catch
                    {
                        flag = false;
                    }

                    string text13 = templateBlock.Substring(num, num3 - num);
                    text4 = ((!_hstValues.ContainsKey(text5) || !flag) ? (text4 + text13 + ParseConditions(text9.Trim(), 1)) : (text4 + text13 + ParseConditions(text8.Trim(), 1)));
                    num2 = num6 + text7.Length;
                    num = num2;
                }

                return text4 + templateBlock.Substring(num);
            }
            catch (Exception exception)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"Bloque: {templateBlock.Substring(num2, templateBlock.Length - num2)}.", exception);
                }

                return "";
            }
        }

        private string ParseVariables(string parsedBlock)
        {
            int startIndex = 0;
            while ((startIndex = parsedBlock.IndexOf(VariableTagBegin, startIndex)) != -1)
            {
                int num = parsedBlock.IndexOf(VariableTagEnd, startIndex + VariableTagBegin.Length);
                if (num == -1)
                {
                    throw new Exception($"Index {startIndex}: could not find Variable End Tag");
                }

                string text = parsedBlock.Substring(startIndex + VariableTagBegin.Length, num - startIndex - VariableTagBegin.Length);
                string[] array = text.Split(ModificatorTag.ToCharArray());
                if (text.IndexOf(ModificatorInstance) >= 0)
                {
                    text = array[0];
                    text = text + "_" + _hstValues[InstanceVariable].ToString();
                }
                else
                {
                    text = array[0];
                }

                string Value = string.Empty;
                if (_hstValues.ContainsKey(text) && _hstValues[text] != null)
                {
                    Value = _hstValues[text].ToString();
                }

                for (int i = 1; i < array.Length; i++)
                {
                    if (array[i] != ModificatorInstance)
                    {
                        ApplyModificator(ref Value, array[i]);
                    }
                }

                parsedBlock = parsedBlock.Substring(0, startIndex) + Value + parsedBlock.Substring(num + VariableTagEnd.Length);
                startIndex += Value.Length;
            }

            return parsedBlock;
        }

        private string ParseSubtemplates(string parsedBlock)
        {
            int startIndex = 0;
            while ((startIndex = parsedBlock.IndexOf(SubTemplateTagBegin, startIndex)) != -1)
            {
                int num = parsedBlock.IndexOf(SubTemplateTagEnd, startIndex + SubTemplateTagBegin.Length);
                if (num == -1)
                {
                    throw new Exception($"Index {startIndex}: could not find Variable End Tag");
                }

                string text = parsedBlock.Substring(startIndex + SubTemplateTagBegin.Length, num - startIndex - SubTemplateTagBegin.Length);
                string empty = string.Empty;
                Parser parser = new Parser(SubTemplatePath + text);
                parser.Variables = _hstValues;
                parser.SubTemplatePath = SubTemplatePath;
                empty = parser.Parse(2);
                parsedBlock = parsedBlock.Substring(0, startIndex) + empty + parsedBlock.Substring(num + SubTemplateTagEnd.Length);
                startIndex += empty.Length;
            }

            return parsedBlock;
        }

        private void ApplyModificator(ref string Value, string Modificator)
        {
            string text = "";
            string text2 = "";
            int num;
            if ((num = Modificator.IndexOf("(")) != -1)
            {
                if (!Modificator.EndsWith(")"))
                {
                    throw new Exception("Incorrect modificator expression");
                }

                int num2 = Modificator.Length - 1;
                text = Modificator.Substring(0, num).ToUpper();
                text2 = Modificator.Substring(num + 1, num2 - num - 1);
            }
            else
            {
                text = Modificator.ToUpper();
            }

            string[] array = text2.Split(ModificatorParamSep.ToCharArray());
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = array[i].Trim().Replace("~~", ":").Replace("||", ",")
                    .Replace("{{", "(")
                    .Replace("}}", ")")
                    .Replace("$$", "#");
                if (array[i].StartsWith(ModificatorTagVar))
                {
                    array[i] = _hstValues[array[i].Replace(ModificatorTagVar, "")].ToString();
                }
            }

            try
            {
                Type type = Type.GetType("TemplateParser.Modificators." + text + ",bpmco_Modificators");
                if (type.IsSubclassOf(Type.GetType("TemplateParser.Modificators.Modificator,bpmco_Modificators")))
                {
                    Modificator modificator = (Modificator)Activator.CreateInstance(type);
                    modificator.Apply(ref Value, array);
                }
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                {
                    log.Error($"mod:{text}", ex);
                }

                throw new Exception($"Could not find modificator or error inside '{text}' - Exception: {ex.Message}");
            }
        }
    }
}