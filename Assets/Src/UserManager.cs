using UnityEngine;
using System.Collections;

public class UserManager : MonoBehaviour {

    private int clientId;
    private string session;

    void Awake()
    {

    }
	
    public void UpdateClient(int newClientId, string newSession)
    {
        this.clientId = newClientId;
        this.session = newSession;
    }

	void Update () {
	
	}

    public int getClientId()
    {
        return clientId;
    }

    public string getSession()
    {
        return session;
    }
}
