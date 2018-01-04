using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.IO;

[ExecuteInEditMode]
public class noStaticObject : MonoBehaviour {



		void Awake()
		{
			GameObject relcObject;

			string[] lines = File.ReadAllLines("postions.txt");
			foreach (string line in lines)
			{
				string[] col = line.Split(new char[] { ';' });

				if (col[0] == "null")
				{
					relcObject = GameObject.Find(col[1]);
				}
				else
				{
					relcObject = GameObject.Find(col[0] + "/" + col[1]);
				}


				try
				{
					relcObject.isStatic=false;
					Debug.Log(col[0] + "/" + col[1]);
				}
				catch
				{
					print(col[0] + "/" + col[1]);
				}
			}
		}
	}
