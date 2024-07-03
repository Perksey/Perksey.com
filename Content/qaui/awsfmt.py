#!/usr/bin/env python3
import json
import typing
import requests

questions = []
final_questions = {
  "numQuestions": 65,
  "passMark": 46,
  "numMinutes": 90,
  "categories": {
    "Cloud Concepts": 15,
    "Security and Compliance": 20,
    "Cloud Technology and Services": 22,
    "Billing, Pricing, and Support": 8
  },
  "name": "AWS Cloud Practitioner Practice Test"
}
final_questions["questions"] = {x: [] for x in final_questions["categories"].keys()}
n_exams = 20

URL_FMT = "https://raw.githubusercontent.com/kananinirav/AWS-Certified-Cloud-Practitioner-Notes/7ddd7bedbc1899aa0f08377cc959046ea1996790/practice-exam/practice-exam-{}.md"

question = {}
for i in range(1, 21):
    questions = []
    req = requests.get(URL_FMT.format(str(i)))
    req.raise_for_status()
    for line in req.text.splitlines():
        if not isinstance(line, str):
            continue
        if line.find(" ") != 0:
            dot = line.find(". ")
            if dot != 1 and dot != 2:
                continue
            line = line[(dot + 2):]
            
            if "answers" not in question or (len(question["answers"]) != 4 and len(question["answers"]) != 5):
                print("Discarding WIP question: " + json.dumps(question))
            else:
                questions.append(question)
            
            question = {}
            question["text"] = line
        elif " - " in line:
            hyphen = line.find(" - ")
            if hyphen <= 1:
                continue
            dot = line.find(". ")
            if dot != hyphen + 4:
                continue
            if "answers" not in question:
                question["answers"] = []
            question["answers"].append({"text": line[dot + 1:].strip(), "letter": line[dot - 1:].strip()[0], "correct": False})
        else:
            correct_ans = line.find("Correct answer: ")
            if correct_ans < 1:
                continue
            correct_ans += len("Correct answer: ")
            for correct in line[correct_ans:].split(","):
                correct = correct.strip()
                for i in range(0, len(question["answers"])):
                    if question["answers"][i]["letter"] == correct:
                        question["answers"][i]["correct"] = True

    if "answers" not in question or (len(question["answers"]) != 4 and len(question["answers"]) != 5):
        print("Discarding WIP question: " + json.dumps(question))
    else:
        questions.append(question)

    q_text = [x["text"] for x in questions]
    c_text = list(final_questions["categories"].keys())
    comma_categories = input("Please provide a comma separated list of question categories for the given question list for an Amazon Web Services Cloud Practitioner exam, where each category is an index in the following array: \"" + "\", \"".join(c_text) + f"\". Provide ONLY an INDEX (i.e. 0 inclusive lower bound, {len(c_text)}) exclusive upper bound) in a COMMA separated list. If using AI, AIs can provide a short explanation so long as at the end they provide a CODE BLOCK containing ONLY THE COMMA SEPARATED INDICES. The question list: \"" + "\", \"".join(q_text) + "\".")
    for i, x in enumerate(comma_categories.split(",")):
        final_questions["questions"][c_text[int(x.strip())]].append(next(q for q in questions if q["text"] == q_text[i]))

for qs in final_questions["questions"].values():
    start = 0
    while start != -1:
        start = -1
        for i, q in enumerate(qs, start):
            n_correct = sum(1 for x in q["answers"] if x["correct"])
            if n_correct != 1 and ("two" not in q["text"].lower() or n_correct != 2) and ("three" not in q["text"].lower() or n_correct != 3):
                print("Discarding question with incorrect number of correct answers: " + json.dumps(q))
                qs.remove(q)
                start = i
                break

with open("aws.json", "w") as f:
    f.write(json.dumps(final_questions, indent=2))
