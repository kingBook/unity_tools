﻿namespace UnityTools{
	/// <summary>
	/// 方法
	/// </summary>
	public class CSharpMethod:IAlignObject{
		
		/// <summary>
		/// 所在的命名空间
		/// </summary>
		public CSharpNameSpace nameSpace;

		/// <summary>
		/// 返回类型
		/// </summary>
		public IString returnType;

        /// <summary>
		/// <para>单词+尖括号/单词</para>
		/// <para><see cref="Segment"/>/<see cref="WordAngleBrackets"/></para>
		/// <para>如："xxx&lt;...&gt;","xxx"</para>
		/// </summary>
		public IString name;

		public object parameters;

		/// <summary>
		/// 泛型约束列表
		/// </summary>
		public CSharpGenericConstraint[] genericConstraints;
		
	}
}
