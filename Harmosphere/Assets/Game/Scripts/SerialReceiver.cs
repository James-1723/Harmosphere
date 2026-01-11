/*
Unity read + Arduino send example
* DisplayType 可選擇要顯示ASCII碼或翻譯成character
*/
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO.Ports;

public class SerialReceiver : MonoBehaviour {

    [SpaceAttribute(10)]
    [HeaderAttribute("                       -------   ASCII:0    ------- ")]
    [HeaderAttribute("                       ------- Character:1  ------- ")]
    [SpaceAttribute(10)]
    public int DisplayType;


    private SerialManager serialmanager;

    // Reading sentence
    // private const int bufferSize = 100;                                                              
    // private char[] buffer = new char[bufferSize];

    // Read data
    private int data;

    private void Awake () {

        serialmanager = GetComponent<SerialManager>();

    }

    void Update() {

        serialReceive();
        
    }

    private void serialReceive(){

        try {

            data = serialmanager.serialPort.ReadByte();                              // 讀one byte          
            displayStyle(DisplayType, data);

        }
        catch {
        
        }
    }

    private void displayStyle(int displayType, int data){

        if (displayType == 0){                                                       // display ASCII number

            Debug.Log(data);   

        } 
        else if (displayType == 1) {                                                 // display character

            char c = Convert.ToChar(data);                        
            Debug.Log(c);    

        }
    }
}