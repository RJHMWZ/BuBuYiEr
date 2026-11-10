using System;
using System.IO;
using System.Text;
using System.Security.Cryptography;

// Offline, original procedural score and sound design. No samples or external packages.
// Run with the small .NET console project in Temp/AudioSynthesis (44100 Hz PCM masters).
internal static class GenerateGameAudio
{
    private const int Rate = 44100;
    private static string root;
    private sealed class Mix
    {
        public readonly double[] Left,Right;
        public readonly bool Loop;
        public readonly Random Noise;
        public Mix(double seconds,bool loop=false,int seed=7)
        { Left=new double[(int)Math.Round(seconds*Rate)];Right=new double[Left.Length];Loop=loop;Noise=new Random(seed); }
        public void Add(double time,int midi,double duration,double gain,int voice,double pan=0)
        {
            int start=(int)Math.Round(time*Rate),count=(int)Math.Ceiling(duration*Rate);
            double frequency=440*Math.Pow(2,(midi-69)/12.0);
            double gl=Math.Cos((pan+1)*Math.PI*.25),gr=Math.Sin((pan+1)*Math.PI*.25);
            for(int i=0;i<count;i++)
            {
                double t=i/(double)Rate,phase=2*Math.PI*frequency*t;
                double release=Math.Min(1,(duration-t)/.035);release=Math.Max(0,release);
                double value;
                if(voice==0) // felt mallet: bright attack, quickly damped upper modes
                    value=(Math.Sin(phase)*Math.Exp(-t*4.3)+.22*Math.Sin(phase*2.76)*Math.Exp(-t*14)+.055*Math.Sin(phase*5.4)*Math.Exp(-t*25))*Math.Min(1,t/.004);
                else if(voice==1) // soft rounded plucked string
                    value=(Math.Sin(phase)*Math.Exp(-t*3.1)+.22*Math.Sin(phase*2)*Math.Exp(-t*7)+.055*Math.Sin(phase*3)*Math.Exp(-t*12))*Math.Min(1,t/.006);
                else if(voice==2) // warm, slow sine ensemble, no saw wave or harsh filter sweep
                {
                    double env=Math.Pow(Math.Min(1,t/.32),2)*Math.Pow(Math.Min(1,(duration-t)/.6),2);
                    value=(.56*Math.Sin(phase)+.22*Math.Sin(phase*1.0018)+.22*Math.Sin(phase*.9983)+.07*Math.Sin(phase*2))*env;
                }
                else if(voice==3) // bass remains audible on a phone through its quiet second harmonic
                    value=(Math.Sin(phase)+.38*Math.Sin(phase*2)+.09*Math.Sin(phase*3))*Math.Exp(-t*3.4)*Math.Min(1,t/.012);
                else if(voice==5) // 圆润拨弦采用慢起音，为碰撞音保留清晰瞬态。
                    value=(Math.Sin(phase)*Math.Exp(-t*1.65)+.085*Math.Sin(phase*2)*Math.Exp(-t*4))*Math.Min(1,t/.022);
                else if(voice==6) // 中低频暖和弦，长包络，不加高频泛音。
                {
                    double env=Math.Pow(Math.Min(1,t/.95),2)*Math.Pow(Math.Min(1,(duration-t)/1.5),2);
                    value=(Math.Sin(phase)+.035*Math.Sin(phase*2))*env;
                }
                else // soft glass, tuned harmonic partials
                    value=(Math.Sin(phase)*Math.Exp(-t*5)+.16*Math.Sin(phase*2)*Math.Exp(-t*12)+.035*Math.Sin(phase*4)*Math.Exp(-t*20))*Math.Min(1,t/.003);
                AddSample(start+i,value*gain*release,gl,gr);
            }
        }
        public void Drum(double time,int kind,double gain,double pan=0)
        {
            double duration=kind==0?.20:kind==1?.08:.045;
            int start=(int)Math.Round(time*Rate),count=(int)(duration*Rate);
            double gl=Math.Cos((pan+1)*Math.PI*.25),gr=Math.Sin((pan+1)*Math.PI*.25),noise=0;
            for(int i=0;i<count;i++)
            {
                double t=i/(double)Rate;
                noise+=.18*(Noise.NextDouble()*2-1-noise);
                double v=kind==0?Math.Sin(2*Math.PI*(66*t+32*.025*(1-Math.Exp(-t/.025))))*Math.Exp(-t*27):
                    kind==1?(Math.Sin(2*Math.PI*430*t)*.72+noise*.35)*Math.Exp(-t*75):noise*Math.Exp(-t*95);
                AddSample(start+i,v*gain*Math.Min(1,t/.0015)*Math.Min(1,(duration-t)/.005),gl,gr);
            }
        }
        private void AddSample(int index,double value,double gl,double gr)
        {
            if(Loop)index%=Left.Length;
            if(index<0 || index>=Left.Length)return;
            Left[index]+=value*gl;Right[index]+=value*gr;
        }
        public void Room(double amount)
        {
            var l=(double[])Left.Clone();var r=(double[])Right.Clone();
            double[] delays={.071,.113,.173,.257,.347},gains={.31,.23,.15,.09,.045};
            for(int d=0;d<delays.Length;d++)
            {
                int delay=(int)(delays[d]*Rate);
                for(int i=0;i<Left.Length;i++)
                {
                    int j=i-delay;if(j<0){if(!Loop)continue;j+=Left.Length;}
                    Left[i]+=(d%2==0?r[j]:l[j])*gains[d]*amount;
                    Right[i]+=(d%2==0?l[j]:r[j])*gains[d]*amount;
                }
            }
        }
        public void Save(string name,bool music,double targetPeak,double targetRms=.075)
        {
            int channels=music?2:1;
            double meanL=0,meanR=0,peak=0,squares=0;
            for(int i=0;i<Left.Length;i++){meanL+=Left[i];meanR+=Right[i];}
            meanL/=Left.Length;meanR/=Right.Length;
            for(int i=0;i<Left.Length;i++)
            {
                Left[i]-=meanL;Right[i]-=meanR;
                if(!music){Left[i]=(Left[i]+Right[i])*.7071067811865476;Right[i]=Left[i];}
                peak=Math.Max(peak,Math.Max(Math.Abs(Left[i]),Math.Abs(Right[i])));
                squares+=Left[i]*Left[i]+Right[i]*Right[i];
            }
            double rms=Math.Sqrt(squares/(Left.Length*2));
            double scale=music?Math.Min(targetPeak/peak,targetRms/rms):targetPeak/peak;
            string path=Path.Combine(root,name+".wav");
            using(var file=new BinaryWriter(File.Create(path)))
            {
                int bytes=Left.Length*channels*2;
                file.Write(Encoding.ASCII.GetBytes("RIFF"));file.Write(36+bytes);file.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                file.Write(16);file.Write((short)1);file.Write((short)channels);file.Write(Rate);file.Write(Rate*channels*2);
                file.Write((short)(channels*2));file.Write((short)16);file.Write(Encoding.ASCII.GetBytes("data"));file.Write(bytes);
                for(int i=0;i<Left.Length;i++)
                {
                    double fade=Loop?1:Math.Min(1,Math.Min(i/(Rate*.003),(Left.Length-1-i)/(Rate*.012)));
                    file.Write((short)Math.Clamp(Math.Round(Left[i]*scale*fade*32767),-32767,32767));
                    if(channels==2)file.Write((short)Math.Clamp(Math.Round(Right[i]*scale*fade*32767),-32767,32767));
                }
            }
            WriteMeta(path,music);
            Console.WriteLine($"{name}: {Left.Length/(double)Rate:F2}s, peak {20*Math.Log10(peak*scale):F1} dBFS, rms {20*Math.Log10(rms*scale):F1} dBFS");
        }
    }
    private static void QuietSong(string name,int bpm,int variation)
    {
        double beat=60.0/bpm;
        var m=new Mix(64*beat,true,107+variation);
        // 每两小节才换和弦，少量单音应答为球的碰撞反馈留出空间。
        int[][] voicings={
            new[]{48,55,64},new[]{48,57,64},new[]{48,57,65},new[]{50,55,62},
            new[]{48,55,64},new[]{47,55,64},new[]{50,57,65},new[]{50,55,62}
        };
        int[][] motifs={
            new[]{64,67,64,62,64,67,65,62},
            new[]{67,64,65,62,64,59,65,62},
            new[]{64,69,65,67,64,67,69,62},
            new[]{67,64,69,62,67,64,65,62},
            new[]{64,67,65,62,67,64,65,62},
            new[]{64,64,65,62,67,64,65,62}
        };
        int[] motif=motifs[variation];
        for(int phrase=0;phrase<8;phrase++)
        {
            double time=phrase*8*beat;
            int[] chord=voicings[phrase];
            for(int c=0;c<chord.Length;c++)
                m.Add(time+c*.05,chord[c],8*beat+.8,c==0?.037:.029,6,(c-1)*.22);
            m.Add(time+(1.5+variation*.12)*beat,motif[phrase],2.9,.075,5,-.14);
            // 两个乐句只保留和弦与单音，让循环有呼吸感。
            if(phrase!=2 && phrase!=6)
                m.Add(time+5.5*beat,chord[1]+12,2.6,.047,5,.14);
        }
        m.Room(.24);
        m.Save(name,true,.23,.045);
    }
    private static void Sequence(string name,int[] notes,double step,double peak,int voice=4,double length=.22)
    {
        var m=new Mix(step*(notes.Length-1)+length+.2);
        for(int i=0;i<notes.Length;i++)m.Add(step*i,notes[i],length,1.0/(1+i*.10),voice,(i%2==0?-.1:.1));
        m.Room(.30);m.Save(name,false,peak);
    }
    private static void Effects()
    {
        Sequence("UI_Click",new[]{76},0,.16,1,.085);
        Sequence("UI_Open",new[]{67,76},.055,.16,0,.14);
        Sequence("UI_Close",new[]{74,67},.045,.13,1,.12);
        Sequence("WallBounce",new[]{60},0,.085,1,.065);
        for(int i=0;i<3;i++)
        {
            Sequence("BrickHit"+(i+1),new[]{72+i*2},0,.115,0,.075);
            var m=new Mix(.30,false,77+i);
            m.Add(0,72+i*2,.13,1,1);m.Add(.032,79+(i==1?2:0),.16,.43,0);m.Add(.067,84,.18,.25,4);
            m.Drum(.008,2,.23);m.Room(.18);m.Save("BrickBreak"+(i+1),false,.19);
        }
        Sequence("Purchase",new[]{72,76,79},.065,.23,1,.22);
        Sequence("Upgrade",new[]{79,81,84,88},.065,.25,0,.28);
        Sequence("RoundStart",new[]{72,76,79,84},.072,.21,1,.22);
        Sequence("Pause",new[]{76,72},.055,.14,0,.16);
        Sequence("Resume",new[]{72,79},.055,.15,0,.17);
        Sequence("Coin",new[]{84,88},.052,.17,4,.19);
        Sequence("Star",new[]{79,84},.08,.19,4,.28);
        Sequence("Reward",new[]{72,76,79,84,88},.10,.25,0,.35);
        Sequence("StoryUnlock",new[]{76,79,84,83,84},.105,.23,0,.36);
        var win=new Mix(1.65);
        foreach(int p in new[]{60,64,67,71})win.Add(.08,p,1.35,.26,2);
        int[] line={72,76,79,81,84,88,84};
        for(int i=0;i<line.Length;i++)win.Add(i*.12,line[i],.5,.65,0);
        win.Room(.4);win.Save("LevelComplete",false,.29);
        var fail=new Mix(1.30);
        foreach(int p in new[]{57,60,64})fail.Add(.06,p,1.15,.2,2);
        int[] fall={79,76,74,72};
        for(int i=0;i<fall.Length;i++)fail.Add(i*.16,fall[i],.48,.5,1);
        fail.Room(.32);fail.Save("LevelFail",false,.20);
    }
    private static string GuidFor(string path)
    {
        string relative=Path.GetRelativePath(Directory.GetCurrentDirectory(),path).Replace('\\','/');
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("BuBuYiEr/original-audio/v1/"+relative))).Substring(0,32).ToLowerInvariant();
    }
    private static void WriteMeta(string path,bool music)
    {
        string guid=File.Exists(path+".meta")?ReadGuid(path+".meta"):GuidFor(path);
        string settings=$"    serializedVersion: 2\n    loadType: {(music?2:0)}\n    sampleRateSetting: 0\n    sampleRateOverride: 44100\n    compressionFormat: {(music?1:0)}\n    quality: {(music?"0.45":"1")}\n    conversionMode: 0\n    preloadAudioData: {(music?0:1)}\n";
        File.WriteAllText(path+".meta","fileFormatVersion: 2\nguid: "+guid+"\nAudioImporter:\n  externalObjects: {}\n  serializedVersion: 8\n  defaultSettings:\n"+settings+
            "  platformSettingOverrides: {}\n  forceToMono: "+(music?0:1)+"\n  normalize: 0\n  loadInBackground: "+(music?1:0)+"\n  ambisonic: 0\n  3D: 0\n  userData: Original procedural composition; generated by Tools/Audio/GenerateGameAudio.cs\n  assetBundleName: \n  assetBundleVariant: \n");
    }
    private static string ReadGuid(string meta)
    {
        foreach(string line in File.ReadAllLines(meta))if(line.StartsWith("guid: "))return line.Substring(6);
        throw new InvalidDataException(meta);
    }
    private static void EnsureFolder(string path)
    {
        if(Directory.Exists(path))return;
        EnsureFolder(Path.GetDirectoryName(path));Directory.CreateDirectory(path);
        File.WriteAllText(path+".meta","fileFormatVersion: 2\nguid: "+GuidFor(path)+"\nfolderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n");
    }
    public static void Main(string[] args)
    {
        root=Path.GetFullPath("Assets/BuBuYiEr/Audio/Resources/BearAudio");EnsureFolder(root);
        QuietSong("HomeLoop",72,5);
        QuietSong("CandyDay",72,0);QuietSong("MilkTea",70,1);QuietSong("Garden",74,2);QuietSong("StarryCandy",68,3);
        QuietSong("BigCandy",72,4);
        if(Array.IndexOf(args,"--music-only")<0)Effects();
    }
}
