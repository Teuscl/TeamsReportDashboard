import json
result_file_name = 'results.jsonl'

results = []
with open(result_file_name, 'r') as file:
    for line in file:
        # Parsing the JSON string into a dict and appending to the list of results
        json_object = json.loads(line.strip())
        results.append(json_object)
 
with open("respostas.txt", "w", encoding="utf-8") as f:
    for res in results:
        result = res['response']['body']['choices'][0]['message']['content']
        f.write(result + "\n")