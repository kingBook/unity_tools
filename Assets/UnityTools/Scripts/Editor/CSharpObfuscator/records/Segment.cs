﻿using System;

namespace UnityTools{
	/// <summary>
    /// 记录一段字符串在.cs文件字符中的起始索引和长度的结构体
    /// </summary>
    public struct Segment:IString{
		public static readonly Segment none=new Segment();
		
		public int startIndex;
		public int length;
		
		/// <summary>
		/// 创建一个SegmentString结构体。
		/// </summary>
		/// <param name="startIndex">起始索引</param>
		/// <param name="length">长度</param>
		public Segment(int startIndex,int length){
			this.startIndex=startIndex;
			this.length=length;
		}

		public override string ToString(){
			throw new Exception("Please call ToString(string fileString)");
		}

		public string ToString(string fileString){
			return fileString.Substring(startIndex,length);
		}
		
	}
}