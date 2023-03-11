using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimWorldOnlineCity
{
	[DefOf]
	public static class OC_LetterDefOf
	{
		public static LetterDef GoldenLetter;
		public static LetterDef GreyGoldenLetter;
		public static LetterDef PurpleLetter;
		public static LetterDef ToxicLetter;
		public static LetterDef BlueLetter;
		public static LetterDef OrangeLetter;
		public static LetterDef BrownLetter;
		public static LetterDef PinkLetter;
	}
}

//ниже пример дефа письма
/*<? xml version = "1.0" encoding = "utf-8" ?>
   <Defs>
	   <LetterDef>
		   <defName>GoldenLetter</defName>				//имя, которое будет использоваться
		   <color>(51, 255, 0)</color>				//цвет
		   <flashColor>(245, 250, 200) </flashColor>	//цвет сияния вокруг квадратика
		   <flashInterval>90</flashInterval>			//интервал свечения
		   <arriveSound>BanditSound</arriveSound>		//звук, проигрываемый при получении пиьсма

			стандартные звуки
			<arriveSound>LetterArrive</arriveSound>
			<arriveSound>LetterArrive_Good</arriveSound>
			<arriveSound>LetterArrive_BadUrgent</arriveSound>
			<arriveSound>LetterArrive_BadUrgentBig</arriveSound>
	
	   </LetterDef>
   

	   <SoundDef>								//свой деф звука
		   <defName>BanditSound</defName>
		   <context>MapOnly</context>
		   <eventNames/>
		   <subSounds>
			   <li>
				   <onCamera>True</onCamera>
				   <grains>
					   <li Class = "AudioGrain_Clip">
						   <clipPath>Things/bandit</clipPath>
					   </li>
				   </grains>
				   <volumeRange>
					   <min>90.70111</min>
					   <max>97.30627</max>
				   </volumeRange>
				   <pitchRange>
					   <min>1.0976015</min>
					   <max>1.0769372</max>
				   </pitchRange>
			   </li> 
		   </subSounds>
	   </SoundDef>
   </Defs>

*/