using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hero : UnitBase
{
    // Start is called before the first frame update
    void Start()
    {
        GetTileUnder();
        TurnManager.m_instance.AddHeroUnit((Hero)this);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        if(m_Moving)
        {
            Move();
        }
    }
}
