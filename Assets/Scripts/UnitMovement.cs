﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UnitMovement : MonoBehaviour
{
    public float speed;
    List<Vector2Int> path = new List<Vector2Int>();
    int i; //Missä kohdassa path polkua ollaan menossa.
    Rigidbody2D rb;
    Vector2 movingDirection;
    Animator anim;
    AnimatorController animControl;
    UnitStatus unitStatus;
    public Task currentTask;
    public float acceleration;
    public float topSpeed;
    public float repulsionStrength;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        transform.position = CordPosition();
        anim = GetComponent<Animator>();
        animControl = GetComponent<AnimatorController>();
        unitStatus = GetComponent<UnitStatus>();
    }

    private void Update()
    {
        if (path.Count != 0)
        {
            if (currentTask.taskRange != 0)
            {
                //Korjaa etäisyys mittaus vektori2...
                if ((currentTask.objectives[0].transform.position - transform.position).magnitude <= currentTask.taskRange) //Jos etäisyys kohteeseen on vähemmän kuin taskiRange
                {
                    GoalReached();
                    return;
                }
            }
            Vector2 vectorToNextPoint = path[i + 1] + new Vector2(0.5f, 0.5f) - (Vector2)transform.position;
            if (Vector2.Dot(vectorToNextPoint, movingDirection) <= 0)
            {
                i++;
                if (i == path.Count - 1)
                {
                    GoalReached();
                }
                else
                {
                    movingDirection = (path[i + 1] - path[i]);
                    transform.rotation = UF.TurnUnit(movingDirection, -90f);
                }
            }
        }
    }

    private void FixedUpdate()
    {
        if (path.Count > 0)
        {
            rb.AddForce(rb.mass * movingDirection.normalized * acceleration * rb.drag);
        }
        if (rb.velocity.magnitude > topSpeed)
        {
            rb.velocity = rb.velocity.normalized * topSpeed;
        }
    }

    void GoalReached()
    {
        rb.velocity = Vector2.zero;
        path.Clear();
        anim.SetBool("Running", false);
        StartTask();
    }

    void StartTask()
    {
        if (currentTask.taskName != GM.tasks[TaskTypes.idle])
        {
            currentTask.taskScriptInstance = (MonoBehaviour)gameObject.AddComponent(Type.GetType(currentTask.taskName)); //Lisätään task nimellä löytyvä koodi jäbälle...
        }
    }

    Vector2 CordPosition()
    {
        return new Vector2((int)transform.position.x + 0.5f, (int)transform.position.y + 0.5f);
    }

    public void Move(List<Vector2Int> newPath, Task newTask)
    {
        if (newPath.Count != 0 && newPath[0] == new Vector2Int(-999, -999)) //ei pitäisi oikeasti koskaan päässä tänne?
        {
            return;
        }
        ResetTaskInstance();
        animControl.ResetAnimations();
        currentTask = newTask;
        path = newPath;
        if (path.Count > 1)
        {
            i = 0;
            anim.SetBool("Running", true);
            movingDirection = (path[1] - path[0]);
            transform.rotation = UF.TurnUnit(movingDirection, -90f);
        }
        else
        {
            path.Clear();
            StartTask();
        }
    }

    public void SetAndStartTask(Task newTask)
    {
        currentTask = newTask;
        StartTask();
    }

    public void Stop()
    {
        path.Clear();
        ResetTaskInstance();
        animControl.ResetAnimations();
        rb.velocity = Vector2.zero;
        currentTask = new Task(GM.tasks[TaskTypes.idle], null);
        unitStatus.currentState = UnitStates.idle;
    }

    void ResetTaskInstance()
    {
        if (currentTask != null && currentTask.taskScriptInstance != null)
        {
            Destroy(currentTask.taskScriptInstance);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Unit")
        {
            Vector2 awayFromOther = transform.position - collision.transform.position;
            rb.AddForce(awayFromOther.normalized * Time.deltaTime * repulsionStrength * rb.mass * rb.drag);
        }
    }
}