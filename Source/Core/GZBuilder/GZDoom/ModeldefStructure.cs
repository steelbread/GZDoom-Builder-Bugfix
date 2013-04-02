﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

using SlimDX;
using SlimDX.Direct3D9;

using CodeImp.DoomBuilder.GZBuilder.Data;

namespace CodeImp.DoomBuilder.GZBuilder.GZDoom {
    internal sealed class ModeldefStructure {
        private const int MAX_MODELS = 4; //maximum models per modeldef entry, zero-based

        internal ModeldefEntry Parse(ModeldefParser parser) {
            string[] textureNames = new string[4];
            string[] modelNames = new string[4];
            string path = "";
            Vector3 scale = new Vector3(1, 1, 1);
            float zOffset = 0;
            float angleOffset = 0;
            float pitchOffset = 0;
            float rollOffset = 0;

            string token;
            bool gotErrors = false;

            //read modeldef structure contents
            while (parser.SkipWhitespace(true)) {
                token = parser.ReadToken();

                if (!string.IsNullOrEmpty(token)) {
                    token = parser.StripTokenQuotes(token).ToLowerInvariant(); //ANYTHING can be quoted...
//path
                    if (token == "path") {
                        parser.SkipWhitespace(true);
                        path = parser.StripTokenQuotes(parser.ReadToken()).Replace("/", "\\");

                        if (string.IsNullOrEmpty(path)) {
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected path to model, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }
//model
                    } else if (token == "model") {
                        parser.SkipWhitespace(true);

                        //model index
                        int modelIndex;
                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out modelIndex)) {
                            // Not numeric!
							General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected model index, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }

                        if (modelIndex >= MAX_MODELS) {
							General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": GZDoom doesn't allow more than " + MAX_MODELS + " per MODELDEF entry!");
                            break;
                        }

						parser.SkipWhitespace(true);

						//model path
                        token = parser.StripTokenQuotes(parser.ReadToken()).ToLowerInvariant();
                        if (string.IsNullOrEmpty(token)) {
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected model name, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        } else {
                            //check extension
                            int dotPos = token.LastIndexOf(".");
                            string fileExt = token.Substring(token.LastIndexOf("."), token.Length - dotPos);
                            if (fileExt != ".md3" && fileExt != ".md2") {
                                General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": model '" + token + "' not parsed. Only MD3 and MD2 models are supported.");
                                gotErrors = true;
                                break;
                            }

                            //GZDoom allows models with identical modelIndex, it uses the last one encountered
                            modelNames[modelIndex] = Path.Combine(path, token);
                        }
//skin
                    } else if (token == "skin") {
                        parser.SkipWhitespace(true);

                        //skin index
                        int skinIndex;
                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out skinIndex)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected skin index, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }

                        if (skinIndex >= MAX_MODELS) {
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": GZDoom doesn't allow more than " + MAX_MODELS + " per MODELDEF entry!");
                            break;
                        }

						parser.SkipWhitespace(true);

						//skin path
                        token = parser.StripTokenQuotes(parser.ReadToken()).ToLowerInvariant();
                        if (string.IsNullOrEmpty(token)) {
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected skin name, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        } else {
                            //check extension
							string ext = Path.GetExtension(token);
							if(Array.IndexOf(TextureData.SUPPORTED_TEXTURE_EXTENSIONS, ext) == -1) {
								General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": image format '" + ext + "' is not supported!");
								textureNames[skinIndex] = TextureData.INVALID_TEXTURE;
							} else {
								//GZDoom allows skins with identical modelIndex, it uses the last one encountered
								textureNames[skinIndex] = Path.Combine(path, token);
							}
                        }
//scale
                    } else if (token == "scale") {
                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref scale.X)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected scale X value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }

                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref scale.Y)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected scale Y value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }

                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref scale.Z)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected scale Z value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }
//zoffset
                    } else if (token == "zoffset") {
                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref zOffset)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected ZOffset value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }
//angleoffset
                    } else if (token == "angleoffset") {
                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref angleOffset)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected AngleOffset value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }
//pitchoffset
                    } else if (token == "pitchoffset") {
                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref pitchOffset)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected PitchOffset value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }
//rolloffset
                    } else if (token == "rolloffset") {
                        parser.SkipWhitespace(true);

                        token = parser.StripTokenQuotes(parser.ReadToken());
                        if (!parser.ReadSignedFloat(token, ref rollOffset)) {
                            // Not numeric!
                            General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected RollOffset value, but got '" + token + "'");
                            gotErrors = true;
                            break;
                        }
//frameindex
					} else if(token == "frameindex" || token == "frame") {
                        //parsed all required fields. if got more than one model - find which one(s) should be displayed 
                        int len = modelNames.GetLength(0);
                        if (!gotErrors && len > 1) {
                            string spriteLump = null;
                            string spriteFrame = null;
                            bool[] modelsUsed = new bool[MAX_MODELS];

                            //step back
                            parser.DataStream.Seek(-token.Length - 1, SeekOrigin.Current);

                            //here we check which models are used in first encountered lump and frame
                            while (parser.SkipWhitespace(true)) {
                                token = parser.StripTokenQuotes(parser.ReadToken()).ToLowerInvariant();

								if(token == "frameindex" || token == "frame") {
									bool frameIndex = (token == "frameindex");
									parser.SkipWhitespace(true);

                                    //should be sprite lump
                                    token = parser.StripTokenQuotes(parser.ReadToken()).ToLowerInvariant();

                                    if (string.IsNullOrEmpty(spriteLump)) {
                                        spriteLump = token;
                                    } else if (spriteLump != token) { //got another lump
                                        for (int i = 0; i < modelsUsed.Length; i++) {
                                            if (!modelsUsed[i]) {
                                                modelNames[i] = null;
                                                textureNames[i] = null;
                                            }
                                        }
                                        break;
                                    }

                                    parser.SkipWhitespace(true);

                                    //should be sprite frame
                                    token = parser.StripTokenQuotes(parser.ReadToken()).ToLowerInvariant();

                                    if (string.IsNullOrEmpty(spriteFrame)) {
                                        spriteFrame = token;
                                    } else if (spriteFrame != token) { //got another frame
                                        for (int i = 0; i < modelsUsed.Length; i++) {
                                            if (!modelsUsed[i]) {
                                                modelNames[i] = null;
                                                textureNames[i] = null;
                                            }
                                        }
                                        break;
                                    }

                                    parser.SkipWhitespace(true);

                                    //should be model index
                                    token = parser.StripTokenQuotes(parser.ReadToken());

                                    int modelIndex;
                                    if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out modelIndex)) {
                                        // Not numeric!
                                        General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected model index, but got '" + token + "'");
                                        gotErrors = true;
                                        break;
                                    }

                                    if (modelIndex >= MAX_MODELS) {
                                        General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": GZDoom doesn't allow more than " + MAX_MODELS + " per MODELDEF entry!");
                                        gotErrors = true;
                                        break;
                                    }

                                    if (modelNames[modelIndex] == null) {
                                        General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": got model index, which doesn't correspond to any defined model!");
                                        gotErrors = true;
                                        break;
                                    }

                                    modelsUsed[modelIndex] = true;

                                    parser.SkipWhitespace(true);

                                    //should be frame name or index. Currently I have no use for it
                                    token = parser.StripTokenQuotes(parser.ReadToken());

									if(frameIndex) {
										int frame;
										if(!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out frame)) {
											// Not numeric!
											General.ErrorLogger.Add(ErrorType.Error, "Error in " + parser.Source + " at line " + parser.GetCurrentLineNumber() + ": expected model frame, but got '" + token + "'");
											gotErrors = true;
											break;
										}
									}

                                } else {
                                    //must be "}", step back
                                    parser.DataStream.Seek(-token.Length - 1, SeekOrigin.Current);
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }

            //find closing brace, then quit;
            while (parser.SkipWhitespace(true)) {
                token = parser.ReadToken();
                if (token == "}")
                    break;
            }

            if (gotErrors) return null;

            //classname is set in ModeldefParser
            ModeldefEntry mde = new ModeldefEntry();
            mde.Scale = scale;
            mde.zOffset = zOffset;
            mde.AngleOffset = angleOffset * (float)Math.PI / 180.0f;
            mde.RollOffset = rollOffset * (float)Math.PI / 180.0f;
            mde.PitchOffset = pitchOffset * (float)Math.PI / 180.0f;

			for(int i = 0; i < modelNames.Length; i++) {
                if (!string.IsNullOrEmpty(modelNames[i])) {
					mde.TextureNames.Add(string.IsNullOrEmpty(textureNames[i]) ? textureNames[i] : textureNames[i].ToLowerInvariant());
                    mde.ModelNames.Add(modelNames[i].ToLowerInvariant());
                }
            }

            return mde;
        }
    }
}