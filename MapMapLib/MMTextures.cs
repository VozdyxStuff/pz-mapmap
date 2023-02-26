/*******************************************************************
 * Author: Kees "TurboTuTone" Bekkema
 *******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
//using System.Windows.Media.Imaging;

namespace MapMapLib
{
	public class MMTextures
	{
		public bool NewFormat = false;
		public Dictionary<string, MMTextureData> Textures;
		public Dictionary<string, Dictionary<string, MMTextureData>> Sheets;
		public System.Drawing.Imaging.PixelFormat format;

		private StringBuilder strBuilder;

		public MMTextures()
		{
			this.strBuilder = new StringBuilder();
			this.Textures = new Dictionary<string, MMTextureData>();
			this.Sheets = new Dictionary<string, Dictionary<string, MMTextureData>>();
		}

		public void LoadTextureDir(string path){
			if (Directory.Exists(path)){
				foreach (string sheetDir in Directory.GetDirectories(path, "*")){
					string sheetName = sheetDir.Substring(sheetDir.LastIndexOf(Path.DirectorySeparatorChar) + 1);
					Console.WriteLine("Got sheet: {0}", sheetName);
					if (!this.Sheets.ContainsKey(sheetName)){
						this.Sheets.Add(sheetName, new Dictionary<string, MMTextureData>());
					}

					foreach (string textureFile in Directory.GetFiles(sheetDir, "*png")){
						Image img = Image.FromFile(textureFile, true);
						Bitmap bm = new Bitmap(img);
						string textureName = textureFile.Substring(textureFile.LastIndexOf(Path.DirectorySeparatorChar) + 1);
						textureName = (textureName.Split(new Char[] { '.' }))[0];
						MMTextureData tex = new MMTextureData(0, 0, bm.Width, bm.Height, 0, 0, 0, 0, textureName);
						tex.SetData(bm);

						if (!this.Textures.ContainsKey(tex.name)){
							this.Textures.Add(tex.name, tex);
						}
						if (!this.Sheets[sheetName].ContainsKey(tex.name))
							this.Sheets[sheetName].Add(tex.name, tex);
					}
				}
			} else {
				Console.WriteLine("Texture path does not exist: {0}", Path.GetFileName(path));
			}
		}

		public void Load(string path)/*{{{*/
		{
			if (Path.GetExtension(path) == ".pack" && File.Exists(path))
			{
				Console.WriteLine("Reading texture data from: {0}", Path.GetFileName(path));
				using (BinaryReader binReader = new BinaryReader(File.Open(path, FileMode.Open)))
				{
					this.readPackFile(binReader);
				}
			}
			else
			{
				Console.WriteLine("Texture data path invalid or doesnt exist: {0}", Path.GetFileName(path));
			}
		}/*}}}*/
		private void readPackFile(BinaryReader binReader)/*{{{*/
		{
			NewFormat = false;
			int nCount = binReader.ReadInt32();
			if (nCount == 1263557200)
			{
				binReader.ReadInt32();
				nCount = binReader.ReadInt32();
				NewFormat = true;
			}
				
			Console.WriteLine("Sheet count: {0}", nCount);

			for (int nn = 0; nn < nCount; nn++)
			{
				loadFromPackFile(binReader, nn);
			}
		}/*}}}*/
		private string readString(BinaryReader binReader)/*{{{*/
		{
			this.strBuilder.Clear();
			int l = binReader.ReadInt32();
			for (int n = 0; n < l; n++)
			{
				this.strBuilder.Append(binReader.ReadChar());
			}
			return this.strBuilder.ToString();
		}/*}}}*/
		private int readInt(byte[] buffer)/*{{{*/
		{
                        Console.WriteLine("Length of Array: {0,3}", buffer.Length);
			return buffer[0] << 24 | (buffer[1] & 0xFF) << 16 | (buffer[2] & 0xFF) << 8 | buffer[3] & 0xFF;
		}/*}}}*/
		private void loadFromPackFile(BinaryReader binReader, int sn)/*{{{*/
		{
			List<MMTextureData> TempSubTextureInfo = new List<MMTextureData>();
			readString(binReader);
			int numEntries = binReader.ReadInt32();
			if (numEntries == 1263557200)
			{
				binReader.ReadInt32();
				numEntries = binReader.ReadInt32();
				NewFormat = true;

			}

			/* bool mask = */
			binReader.ReadInt32()/* != 0*/;
			for (int n = 0; n < numEntries; n++)
			{
				String entryName = readString(binReader);
				int a = binReader.ReadInt32();
				int b = binReader.ReadInt32();
				int c = binReader.ReadInt32();
				int d = binReader.ReadInt32();
				int e = binReader.ReadInt32();
				int f = binReader.ReadInt32();
				int g = binReader.ReadInt32();
				int h = binReader.ReadInt32();
				TempSubTextureInfo.Add(new MMTextureData(a, b, c, d, e, f, g, h, entryName));
			}
			if (!NewFormat)
			{
				long posPNGstart = binReader.BaseStream.Position;
				binReader.BaseStream.Seek(8, SeekOrigin.Current); //skip header
																  //start reading PNG chunks
				int datalen = 0;
				string chunkid = "";
				do
				{
					datalen = this.readInt(binReader.ReadBytes(4));
					this.strBuilder.Clear();
					for (int n = 0; n < 4; n++)
					{
						this.strBuilder.Append(binReader.ReadChar());
					}
					chunkid = strBuilder.ToString();
					if (chunkid != "IEND") // <- reminder this is advised against
					{
						binReader.BaseStream.Seek(datalen, SeekOrigin.Current);
						binReader.BaseStream.Seek(4, SeekOrigin.Current);
					}
					else
					{
						binReader.BaseStream.Seek(4, SeekOrigin.Current);
					}
				} while (chunkid != "IEND");
				long posPNGend = binReader.BaseStream.Position;

				binReader.BaseStream.Seek(posPNGstart, SeekOrigin.Begin);
				byte[] data = binReader.ReadBytes(Convert.ToInt32(posPNGend - posPNGstart));

				Image img = Image.FromStream(new MemoryStream(data));
				Bitmap bm = new Bitmap(img);
				//bm.Save(OutputDir + "test" + Convert.ToString(sn) + ".png", System.Drawing.Imaging.ImageFormat.Png);
				//if (this.format != bm.PixelFormat)
				//{
				this.format = bm.PixelFormat;
				//}

				foreach (MMTextureData tex in TempSubTextureInfo)
				{
					tex.SetData(bm);
					//add to regular text table
					if (!this.Textures.ContainsKey(tex.name))
						this.Textures.Add(tex.name, tex);
					//add to sheet
					string[] nameparts = tex.name.Split(new Char[] { '_' });
					if (nameparts.Count() == 1)
						nameparts = new string[2] { nameparts[0], "1" }; // small fix for some odd sheets
					string sheetname = nameparts[0] + "_" + nameparts[1];
					if (!this.Sheets.ContainsKey(sheetname))
						this.Sheets.Add(sheetname, new Dictionary<string, MMTextureData>());
					if (!this.Sheets[sheetname].ContainsKey(tex.name))
						this.Sheets[sheetname].Add(tex.name, tex);
				}

				int id = 0;
				do
				{
					id = binReader.ReadInt32();
				} while (id != -559038737);
			}
			else if (NewFormat)
			{
				//New Format reading
				int PNGSize = binReader.ReadInt32();
				long posPNGstart = binReader.BaseStream.Position;
				long posPNGend = posPNGstart + PNGSize;

				binReader.ReadInt32();

					binReader.BaseStream.Seek(posPNGstart, SeekOrigin.Begin);
					byte[] data = binReader.ReadBytes(Convert.ToInt32(posPNGend - posPNGstart));

				Image img = Image.FromStream(new MemoryStream(data));
				Bitmap bm = new Bitmap(img);

				this.format = bm.PixelFormat;

				foreach (MMTextureData tex in TempSubTextureInfo)
				{
					tex.SetData(bm);
					//add to regular text table
					if (!this.Textures.ContainsKey(tex.name))
						this.Textures.Add(tex.name, tex);
					//add to sheet
					string[] nameparts = tex.name.Split(new Char[] { '_' });
					if (nameparts.Count() == 1)
						nameparts = new string[2] { nameparts[0], "1" }; // small fix for some odd sheets
					string sheetname = nameparts[0] + "_" + nameparts[1];
					if (!this.Sheets.ContainsKey(sheetname))
						this.Sheets.Add(sheetname, new Dictionary<string, MMTextureData>());
					if (!this.Sheets[sheetname].ContainsKey(tex.name))
						this.Sheets[sheetname].Add(tex.name, tex);
				}

			}
		}/*}}}*/
	}
}
