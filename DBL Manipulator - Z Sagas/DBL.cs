using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Rainbow.ImgLib.Encoding;
using static Naruto_CCS_Text_Editor.Bin;

namespace DBL_Manipulator___Z_Sagas
{
    public class DBL
    {
        public int FileCount;
        public byte[] Header, DBLB;

        public List<FileBlock> Files;
        public List<Texture> Textures;

        public DBL(byte[] input)
        {
            DBLB = input;
            FileCount = (int)ReadUInt(input, 0, Int.UInt32);

            uint headersize = (uint)ReadUInt(input, 8, Int.UInt32);
            Header = ReadBlock(input, 0, headersize);

            #region File Reading
            Files = new List<FileBlock>();
            for (int i = 0; i < FileCount; i++)
            {
                byte[] entry = ReadBlock(Header, (uint)(0x38 + (i * 0x48)), 0x48);
                if ((int)ReadUInt(entry, 0xC, Int.UInt32) > headersize)
                    Files.Add(new FileBlock(entry));
            }
            #endregion
            #region Texture Reading
            Textures = new List<Texture>();
            foreach (var file in Files)
            {
                Textures.Add(new Texture(DBLB, file));
            }
            #endregion
        }
        public void ExtractContainer(string savefolder)
        {
            foreach (var texture in Textures)
            {
                string pngsave = savefolder + texture.texInfo.FileName + ".png";
                texture.GetPNG().Save(pngsave);
            }
        }
        public static void RepackContainer(string openfolder, string savepath, ProgressBar progress)
        {
            DirectoryInfo info = new DirectoryInfo(openfolder);
            string name = info.Name;

            if (Directory.EnumerateFiles(openfolder).Count() == 0)
                return;//Error

            string[] filelist = Directory.EnumerateFiles(openfolder).ToArray();

            int i = 0;
            foreach (string filexc in filelist)
            {
                string dblname = Path.GetFileNameWithoutExtension(filexc);
                string dblfolder = openfolder+@"\"+dblname;

                byte[] filex = File.ReadAllBytes(filexc);
                try
                {
                    DBL remount = new DBL(filex);
                    if(Directory.Exists(dblfolder))
                    {
                        int t = 0;
                        foreach(var texture in remount.Textures)
                        {
                            Image png = Image.FromFile(dblfolder + @"\" + texture.texInfo.FileName+".png");
                            remount.SetfromPNG(png, t);
                            png.Dispose();
                            t++;
                        }
                        File.WriteAllBytes(savepath+ dblname+".dbl", remount.DBLB);//Save DBL
                    }
                }
                catch (Exception) { }
                #region ProgressBar
                progress.Report((double)i / filelist.Length);
                Thread.Sleep(20);
                #endregion
                i++;
            }
        }
        public void SetfromPNG(Image input, int index)
        {
            var tex = Textures[index];
            var colors = new HashSet<Color>();
            byte[] coresbyte;
            Color[] cores;
            Bitmap bit = new Bitmap(input);
            bit.RotateFlip(RotateFlipType.Rotate180FlipX);
            int colorcount = 0;
            #region Obter cores no eixo cartesiano 2D        
            for (int y = 0; y < bit.Height; y++)
            {
                for (int x = 0; x < bit.Width; x++)
                {
                    colors.Add(bit.GetPixel(x, y));
                }
            }
            cores = new Color[256];
            Array.Copy(colors.ToArray(), 0, cores, 0, colors.Count);
            cores = Texture.swizzlePalette(cores);//BGR+I
            #region Calcular quantia de cores
            if (cores.Length <= 256)
                colorcount = 256;
            else if (cores.Length <= 16)
                colorcount = 16;
            #endregion
            #region Separar cores para array
            coresbyte = new byte[colorcount * 4];//1024 bytes = 256 cores
            for (int i = 0; i < coresbyte.Length; i += 4)
            {
                if ((i / 4) < cores.Length)
                {
                    coresbyte[i] = cores[i / 4].R;
                    coresbyte[i + 1] = cores[i / 4].G;
                    coresbyte[i + 2] = cores[i / 4].B;
                    coresbyte[i + 3] = cores[i / 4].A;
                    if (cores[i / 4].A <= 255)
                        coresbyte[i + 3] = (byte)((cores[i / 4].A * 128) / 255);
                }

            }
            #endregion
            #endregion
            #region Obter índices de pixel no eixo cartesiano 2D
            var pixeldata = new List<byte>();
            Color c1, c2;
            int flagx = bit.Width;
            if (tex.texInfo.Bpp == 4)
                flagx /= 2;
            for (int y = 0; y < bit.Height; y++)
                for (int x = 0; x < flagx; x++)
                {
                    if (tex.texInfo.Bpp == 4)
                    {
                        c1 = bit.GetPixel(x * 2 + 1, bit.Height - y - 1);
                        c2 = bit.GetPixel(x * 2, bit.Height - y - 1);
                        pixeldata.Add((byte)((Texture.FindColorIndex(c1, colors.ToArray()) << 4) + Texture.FindColorIndex(c2, colors.ToArray())));

                    }
                    else
                    {
                        c1 = bit.GetPixel(x, bit.Height - y - 1);
                        pixeldata.Add(Texture.FindColorIndex(c1, colors.ToArray()));
                    }
                }
            #endregion
            tex.Tex = pixeldata.ToArray();
            tex.Clt = coresbyte;

            Array.Copy(tex.Tex, 0, DBLB, tex.texInfo.TextureOffs, tex.texInfo.TexSize);//Tex array
            Array.Copy(tex.Clt, 0, DBLB, tex.texInfo.PaletteOffs, tex.texInfo.PaletteSize);//Palette array
        }
    }

    public class Texture
    {
        public byte[] Tex, Clt;
        public Color[] Palette;
        public FileBlock texInfo;

        public Texture(byte[] input, FileBlock info)
        {
            texInfo = info;

            Tex = ReadBlock(input, (uint)info.TextureOffs, (uint)info.TexSize);
            Clt = ReadBlock(input, (uint)info.PaletteOffs, (uint)info.PaletteSize);
            Palette = GetPalette(Clt);

        }
        public Color[] GetPalette(byte[] entries, int rgba = 4)
        {
            byte[] inptcol = entries;
            var color = new List<Color>();
            for (int i = 0; i < inptcol.Length; i += rgba)
            {
                int r = inptcol[i];
                int g = inptcol[i + 1];
                int b = inptcol[i + 2];
                int a = 0xFF;
                if (rgba == 4)
                    a = inptcol[i + 3];
                if (a <= 128)
                    a = (byte)((a * 255) / 128);
                color.Add(Color.FromArgb(a, r, g, b));
            }
            return unswizzlePalette(color.ToArray());
        }
        public Image GetPNG()
        {
            var decoder = new ImageDecoderIndexed(Tex, texInfo.Width, texInfo.Height, IndexCodec.FromBitPerPixel(texInfo.Bpp), Palette);
            return decoder.DecodeImage();
        }


        #region BGR+I
        public static Color[] unswizzlePalette(Color[] palette)
        {
            if (palette.Length == 256)
            {
                Color[] unswizzled = new Color[palette.Length];

                int j = 0;
                for (int i = 0; i < 256; i += 32, j += 32)
                {
                    copy(unswizzled, i, palette, j, 8);
                    copy(unswizzled, i + 16, palette, j + 8, 8);
                    copy(unswizzled, i + 8, palette, j + 16, 8);
                    copy(unswizzled, i + 24, palette, j + 24, 8);
                }
                return unswizzled;
            }
            else
            {
                return palette;
            }
        }
        public static Color[] swizzlePalette(Color[] palette)
        {
            if (palette.Length == 256)
            {
                Color[] unswizzled = new Color[palette.Length];

                int j = 0;
                for (int i = 0; i < 256; i += 32, j += 32)
                {
                    copySW(palette, i, unswizzled, j, 8);
                    copySW(palette, i + 16, unswizzled, j + 8, 8);
                    copySW(palette, i + 8, unswizzled, j + 16, 8);
                    copySW(palette, i + 24, unswizzled, j + 24, 8);
                }
                return unswizzled;
            }
            else
            {
                return palette;
            }
        }
        private static void copy(Color[] unswizzled, int i, Color[] swizzled, int j, int num)
        {
            for (int x = 0; x < num; ++x)
            {
                unswizzled[i + x] = swizzled[j + x];
            }
        }
        private static void copySW(Color[] unswizzled, int i, Color[] swizzled, int j, int num)
        {
            for (int x = 0; x < num; ++x)
            {
                swizzled[j + x] = unswizzled[i + x];
            }
        }
        #endregion
        public static byte FindColorIndex(Color v, Color[] pal)
        {
            byte index = 0;
            for (byte i = 0; i < pal.Length; i++)
                if (pal[i].R == v.R &&
                    pal[i].G == v.G &&
                    pal[i].B == v.B &&
                    pal[i].A == v.A)
                    return i;

            return index;
        }
    }
    public class FileBlock
    {
        public int ID, Bpp;//UInt64
        public int Width, Height;//Uint16
        public int TextureOffs, TexSize, PaletteOffs, PaletteSize, ColorCount, PW, PH;
        public string FileName;

        public FileBlock(byte[] entryblock)
        {
            ID = (int)ReadUInt(entryblock, 0, Int.UInt64);

            TextureOffs = (int)ReadUInt(entryblock, 0xC, Int.UInt32);
            Width = (int)ReadUInt(entryblock, 0x10, Int.UInt16);
            Height = (int)ReadUInt(entryblock, 0x12, Int.UInt16);
            TexSize = Width * Height;

            PaletteOffs = (int)ReadUInt(entryblock, 0x1C, Int.UInt32);
            PW = (int)ReadUInt(entryblock, 0x20, Int.UInt16);
            PH = (int)ReadUInt(entryblock, 0x22, Int.UInt16);
            ColorCount = PW * PH;

            PaletteSize = 0x400;
            Bpp = 8;
            if (ColorCount == 16)
            {
                PaletteSize = 0x40;
                Bpp = 4;
                TexSize /= 2;
            }

            FileName = Encoding.Default.GetString(ReadStrBlock(entryblock, 0x28));//Encoding.Default.GetString(ReadBlock(entryblock, 0x28, 0x20));


            if (FileName.Contains(@"\"))
                FileName = Path.GetFileNameWithoutExtension(FileName);
        }
    }
}
