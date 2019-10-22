﻿namespace UnityTools{
    using System.Collections.Generic;
    using System.IO;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Xml;
	using UnityEditor;
	using UnityEngine;

	/// <inheritdoc />
	/// <summary>项目导入器窗口UI</summary>
	public class ProjectImporterUI:EditorWindow{

		#region static member
		public static readonly string xmlPath=System.Environment.CurrentDirectory+"/ProjectSettings/importProjects.xml";
		
		private static XmlDocument _xmlDocument;
		private static FileLoader _fileLoader;
		private static bool _isLoadXmlComplete;

		[MenuItem("Tools/ProjectImporter")]
		public static void create(){
			var window=GetWindow(typeof(ProjectImporterUI),false,"ProjectImporter");
			window.minSize=new Vector2(435,120);
			window.Show();
		}

		/// <summary>
		/// 保存xml到本地
		/// </summary>
		public static async void saveXml(){
			if(_xmlDocument==null)return;
			await Task.Run(()=>{
				_xmlDocument.Save(xmlPath);
			});
		}
		
		/// <summary>
		/// 加载xml
		/// </summary>
		public static void loadXml(){
			if(_fileLoader==null){
				_fileLoader=new FileLoader();
			}
			_fileLoader.loadAsync(xmlPath);
			_fileLoader.onCompleteAll+=onloadXmlComplete;
		}

		private static void onloadXmlComplete(byte[][] bytesList){
			_fileLoader.onCompleteAll-=onloadXmlComplete;
			byte[] bytes=bytesList[0];
			if(bytes!=null){
				string xmlString=System.Text.Encoding.UTF8.GetString(bytes);
				_xmlDocument=XmlUtil.createXmlDocument(xmlString,false);
			}
			_isLoadXmlComplete=true;
			
		}

		public static XmlDocument xmlDocument{ get =>_xmlDocument;}
		#endregion
		
		private Vector2 _scrollPosition;
		
		/*private void Awake(){
		}*/
		
		private void OnEnable(){
			loadXml();
		}
		
		private void OnGUI(){
			if(!_isLoadXmlComplete)return;
			EditorGUILayout.BeginVertical();
			{
				_scrollPosition=EditorGUILayout.BeginScrollView(_scrollPosition);
				{
					//表头
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.SelectableLabel("Project Name",GUILayout.MinWidth(100),GUILayout.MaxWidth(150));
					EditorGUILayout.LabelField("Version",GUILayout.Width(65));
					EditorGUILayout.LabelField("Path",GUILayout.MinWidth(100));
					GUILayout.Space(140);
					EditorGUILayout.EndHorizontal();
					//
					if(_xmlDocument!=null){
						var items=_xmlDocument.FirstChild.ChildNodes;
						for(int i=0;i<items.Count;i++){
							XmlNode item=items[i];
							string projectName=item.Attributes["name"].Value;
							string editorVersion=item.Attributes["editorVersion"].Value;
							string projectFolderPath=item.InnerText;
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.TextField(projectName,GUILayout.MinWidth(100),GUILayout.MaxWidth(150));
							EditorGUILayout.LabelField(editorVersion,GUILayout.Width(65));
							EditorGUILayout.TextField(projectFolderPath,GUILayout.MinWidth(100));
							if(GUILayout.Button("Explorer",GUILayout.Width(60))){
								showInExplorer(item,projectFolderPath);
							}
							if(GUILayout.Button("Reimport",GUILayout.Width(60))){
								onReimportProject(item,projectFolderPath,projectName);
							}
							if(GUILayout.Button("X",GUILayout.Width(20))){
								onDeleteProject(item,projectName);
							}
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.Space();
						}
					}
				}
				EditorGUILayout.EndScrollView();
				EditorGUILayout.Space();
				//add project button
				EditorGUILayout.BeginHorizontal();
				{
					EditorGUILayout.Space();
					if(GUILayout.Button("Add Project",GUILayout.Width(80))){
						onAddProject();
					}
				}
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(10);
			}
			EditorGUILayout.EndVertical();

			checkDragAndDropOnGUI();
		}

		private void checkDragAndDropOnGUI(){
			if(mouseOverWindow==this){//鼠标位于当前窗口
				if(Event.current.type==EventType.DragUpdated){//拖入窗口未松开鼠标
					DragAndDrop.visualMode=DragAndDropVisualMode.Generic;//改变鼠标外观
				}else if(Event.current.type==EventType.DragExited){//拖入窗口并松开鼠标
					if(DragAndDrop.paths!=null){
						int len=DragAndDrop.paths.Length;
						for(int i=0;i<len;i++){
							string path=DragAndDrop.paths[i];
							Debug.Log(Directory.Exists(path));
							if(Directory.Exists(path)&&FileUtil2.isUnityProjectFolder(path)){
								addProjectWithUnitProjectPath(path);
							}
						}
					}
				}
			}
		}

		private void showInExplorer(XmlNode item,string projectFolderPath){
			if(Directory.Exists(projectFolderPath)){
				FileUtil2.showInExplorer(projectFolderPath);
			}else{
				displayReassignDialog(item);
				
			}
		}

		/// <summary>
		/// 重新导入项目
		/// </summary>
		/// <param name="item"></param>
		/// <param name="projectFolderPath"></param>
		/// <param name="projectName"></param>
		private void onReimportProject(XmlNode item,string projectFolderPath,string projectName){
			if(Directory.Exists(projectFolderPath)){
				if(FileUtil2.isUnityProjectFolder(projectFolderPath)){
					ProjectImporterEditor.deleteProject(projectName);
					ProjectImporterEditor.importCurrentProjectSettings();
					ProjectImporterEditor.importProject(projectFolderPath);
				}
			}else{
				displayReassignDialog(item);
			}
		}

		/// <summary>
		/// 显示是否重新指定项目路径对话框
		/// </summary>
		/// <param name="item"></param>
		private void displayReassignDialog(XmlNode item){
			bool isYes=EditorUtility.DisplayDialog("Invalid project path","Invalid project path.\nWhether to reassign?","Yes","No");
			if(isYes){
				reassignProjectFolderPath(item);
			}
		}

		/// <summary>
		/// 重新指定项目文件夹
		/// </summary>
		/// <param name="item"></param>
		private void reassignProjectFolderPath(XmlNode item){
			string folderPath=FileUtil2.openSelectUnityProjectFolderPanel();
			if(folderPath!=null){
				if(isAlreadyExists(folderPath)){
					displayAlreadyExistsDialog();
				}else{
					item.InnerText=folderPath;
					saveXml();
				}
			}
		}

		/// <summary>
		/// 显示项目已经存在对话框
		/// </summary>
		private void displayAlreadyExistsDialog(){
			EditorUtility.DisplayDialog("Project already exists","The project already exists","OK");
		}
		
		/// <summary>
		/// 点击项目列表后的delete按钮时
		/// </summary>
		/// <param name="item"></param>
		/// <param name="projectName"></param>
		private void onDeleteProject(XmlNode item,string projectName){
			_xmlDocument.FirstChild.RemoveChild(item);
			saveXml();
			//删除项目
			ProjectImporterEditor.deleteProject(projectName);
		}
		
		/// <summary>点击导入项目按钮时</summary>
		private void onAddProject(){
			string folderPath=FileUtil2.openSelectUnityProjectFolderPanel();
			addProjectWithUnitProjectPath(folderPath);
		}

		/// <summary>
		/// 从指定的unity项目路径添加项目
		/// </summary>
		/// <param name="unityProjectPath">文件夹路径</param>
		private void addProjectWithUnitProjectPath(string unityProjectPath){
			if(unityProjectPath!=null){
				string currentProjectPath=System.Environment.CurrentDirectory.Replace("\\","/");
				bool isImport=true;
				string projectName=unityProjectPath.Substring(unityProjectPath.LastIndexOf('/')+1);
				string editorVersion=getEditorVersion(unityProjectPath);
				if(isAlreadyExists(unityProjectPath)){
					//当尝试导入已经存在的项目时，跳过
					displayAlreadyExistsDialog();
					isImport=false;
				}else if(currentProjectPath==unityProjectPath){
					//当尝试导入当前项目时，跳过
					EditorUtility.DisplayDialog("Selection error","Cannot import the project itself.","OK");
					isImport=false;
				}else if(isAlreadyExistsName(projectName)){
					projectName=getRename(projectName);
				}
				if(isImport){
					//导入当前项目的项目设置
					ProjectImporterEditor.importCurrentProjectSettings();
					//导入指定项目
					ProjectImporterEditor.importProject(unityProjectPath);
					//记录已添加的项目到xml
					addItemToXml(unityProjectPath,projectName,editorVersion);
				}
			}
		}

		/// <summary>
		/// 返回项目编辑器版本号
		/// </summary>
		/// <param name="folderPath">unity项目路径</param>
		/// <returns></returns>
		private string getEditorVersion(string folderPath){
			string projectVersionTxtPath=folderPath+"/ProjectSettings/ProjectVersion.txt";
			List<string> filelines=FileUtil2.getFileLines(projectVersionTxtPath,false,1);
			string editorVersionLine=filelines[0];//第一行是版本号
			Regex regex=new Regex(@"m_EditorVersion:\s*",RegexOptions.Compiled);
			Match match=regex.Match(editorVersionLine);
			string versionString=editorVersionLine.Substring(match.Value.Length);
			return versionString;
		}

		/// <summary>
		/// 返回重命名字符
		/// </summary>
		/// <param name="projectName"></param>
		/// <returns></returns>
		private string getRename(string projectName){
			string endNumberString=StringUtil.getEndNumberString(projectName);
			if(string.IsNullOrEmpty(endNumberString)){
				projectName+="1";
			}else{
				string head=projectName.Substring(projectName.Length-endNumberString.Length);
				projectName=head+(int.Parse(endNumberString)+1);
			}
			return projectName;
		}
		
		/// <summary>
		/// 添加一个项到xml
		/// </summary>
		/// <param name="projectFolderPath">项目路径</param>
		/// <param name="projectName">项目名称</param>
		/// <param name="editorVersion">编辑器版本号</param>
		private void addItemToXml(string projectFolderPath,string projectName,string editorVersion){
			if(_xmlDocument==null){
				_xmlDocument=XmlUtil.createXmlDocument(false);
				_xmlDocument.AppendChild(_xmlDocument.CreateElement("Root"));
			}
			var itemElement=_xmlDocument.CreateElement("Item");
			itemElement.SetAttribute("name",projectName);
			itemElement.SetAttribute("editorVersion",editorVersion);
			itemElement.SetAttribute("obfuscated","No");
			itemElement.InnerText=projectFolderPath;
			_xmlDocument.FirstChild.AppendChild(itemElement);
			saveXml();
		}

		/// <summary>
		/// 判定一个项目是否已经存在列表中(项目名称和路径都相同)
		/// </summary>
		/// <param name="projectFolderPath"></param>
		/// <returns></returns>
		private bool isAlreadyExists(string projectFolderPath){
			if(_xmlDocument!=null){
				XmlNodeList items=_xmlDocument.FirstChild.ChildNodes;
				int len=items.Count;
				for(int i=0;i<len;i++){
					if(items[i].InnerText==projectFolderPath){
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// 判定项目名称是否已经存在列表中(只判定名称相同)
		/// </summary>
		/// <param name="projectName"></param>
		/// <returns></returns>
		private bool isAlreadyExistsName(string projectName){
			if(_xmlDocument!=null){
				XmlNodeList items=_xmlDocument.FirstChild.ChildNodes;
				int len=items.Count;
				for(int i=0;i<len;i++){
					if(items[i].Attributes["name"].Value==projectName){
						return true;
					}
				}
			}
			return false;
		}
		
		private void OnDisable(){
			_isLoadXmlComplete=false;
			saveXml();
		}
		
		/// <summary>关闭窗口</summary>
		private void OnDestroy(){
			
		}
	}
}