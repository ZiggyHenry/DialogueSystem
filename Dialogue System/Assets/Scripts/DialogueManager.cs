using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.PlasticSCM.Editor.WebApi;
using UnityEditor.Rendering;
using UnityEngine;

public class DialogueManager : Singleton<DialogueManager>
{
    public DialogueDatabase Database;

    private float dialogueWaitTime;
    private const float kDefaultWaitTime = 2.0f;
    private DialogueLineData currentDialogue;
    private float currentTime = 0.0f;

    Dictionary<string, DialogueLineData> dialogueDatabase = new Dictionary<string, DialogueLineData>();

    public void Start()
    {
        loadDataBase(Database);
    }

    public void loadDataBase(DialogueDatabase database)
    {
        foreach (DialogueLineData line in database.data)
        {
            dialogueDatabase.Add(line.name, line);
        }
    }

    public void StartDialogue(string dialogueName)
    {
        DialogueLineData data = null;

        if (dialogueDatabase.TryGetValue(dialogueName, out data))
        {
            if (data != currentDialogue)
            StartDialogue(data);
        }
    }

    public void StartDialogue(DialogueLineData lineToStart)
    {
        //show Text on screen
        ShowDialogueText message = new ShowDialogueText();
        message.text = lineToStart.text;
        message.id = lineToStart.character;

        EvtSystem.EventDispatcher.Raise<ShowDialogueText>(message);

        //play dialogue Audio
        if (lineToStart.dialogueAudio != null)
        {
            PlayAudio audioMessage = new PlayAudio();
            audioMessage.clipToPlay = lineToStart.dialogueAudio;

            EvtSystem.EventDispatcher.Raise<PlayAudio>(audioMessage);

            dialogueWaitTime = lineToStart.dialogueAudio.length;
        }
        else
        {
            dialogueWaitTime = kDefaultWaitTime;
        }
        currentDialogue = lineToStart;
        currentTime = 0.0f;
    }

    private void PlayResponseLine(int currentResponseIndex)
    {
        EvtSystem.EventDispatcher.Raise<DisableUI>(new DisableUI());
        if (currentDialogue.responses.Length > currentResponseIndex)
        {
            DialogueLineData line = currentDialogue.responses[currentResponseIndex];
            StartDialogue(line);
        }
    }

    private void CreateResponseMessage()
    {
        int numResponses = currentDialogue.responses.Length;
        if (numResponses > 0)
        {
            ShowResponses responseMessage = new ShowResponses();
            responseMessage.responses = new responseData[numResponses];

            int index = 0;
            foreach (DialogueLineData response in currentDialogue.responses)
            {
                responseMessage.responses[index].text = response.text;
                responseMessage.responses[index].karmaScore = response.karmaScore;

                int currentIndex = index;
                responseMessage.responses[index].buttonAction = () => { this.PlayResponseLine(currentIndex); };
                index++;
            }


            EvtSystem.EventDispatcher.Raise<ShowResponses>(responseMessage);
        }
        else
        {
            EvtSystem.EventDispatcher.Raise<DisableUI>(new DisableUI());
            currentDialogue = null;
        }
    }

    public void Update()
    {
        if (currentDialogue != null && dialogueWaitTime > 0.0f)
        {
            currentTime += Time.deltaTime;
            if (currentTime >= dialogueWaitTime)
            {
                dialogueWaitTime = 0.0f;
                CreateResponseMessage();
            }
        }

        if (Input.GetKeyUp(KeyCode.E))
        {
            StartDialogue("dialogue1");
        }
    }
}
