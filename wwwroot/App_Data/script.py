from transformers import AutoModelForSequenceClassification, AutoTokenizer, AutoConfig
import numpy as np
from scipy.special import softmax
import unicodedata
import pandas as pd
import re
import sys
import transformers
import logging

transformers.logging.set_verbosity_error()

def remove_emojis(text):
    return ''.join(c for c in text if not unicodedata.category(c).startswith('So'))

def preprocess(text):
    return text

def split_text_by_sentences(text, window_size=500):
    sentences = re.split(r'(?<=[.!?])\s+', text)
    sentences = [s.strip() for s in sentences if s.strip()]
    
    chunks = []
    chunk = ""

    for sentence in sentences:
        if len(sentence) > window_size:
            words = sentence.split()
            sub_chunks = []
            sub_chunk = ""
            for word in words:
                if len(sub_chunk) + len(word) + 1 <= window_size:
                    sub_chunk = f"{sub_chunk} {word}".strip()
                else:
                    sub_chunks.append(sub_chunk)
                    sub_chunk = word
            if sub_chunk:
                sub_chunks.append(sub_chunk)
            
            if chunk:
                chunks.append(chunk)
            chunk = sub_chunks.pop()
            chunks.extend(sub_chunks)
        elif len(chunk) + len(sentence) + 1 <= window_size:
            chunk = f"{chunk} {sentence}".strip()
        else:
            chunks.append(chunk)
            chunk = sentence

    if chunk:
        chunks.append(chunk)

    return chunks

if __name__ == "__main__":
    input_file = sys.argv[1]  # Cesta k vstupnímu souboru
    output_file = sys.argv[2]  # Cesta k výstupnímu souboru
    

    with open(input_file, "r", encoding="utf-8") as file:
        text = file.read().strip()

    if not text:
        raise ValueError("Soubor neobsahuje žádná data.")

    text = preprocess(text)

    MODEL = "cardiffnlp/twitter-roberta-base-sentiment-latest"
    tokenizer = AutoTokenizer.from_pretrained(MODEL)
    config = AutoConfig.from_pretrained(MODEL)
    model = AutoModelForSequenceClassification.from_pretrained(MODEL)

    chunks = split_text_by_sentences(text)
    pos_total, neu_total, neg_total = 0, 0, 0
    num_chunks = len(chunks)

    if num_chunks == 0:
        raise ValueError("Text se nepodařilo rozdělit na části.")

    for chunk in chunks:
        chunk = chunk[:512]  # Oříznutí, pokud je text příliš dlouhý
        encoded_input = tokenizer(chunk, return_tensors='pt', truncation=True, max_length=512)
        output = model(**encoded_input)
        scores = output[0][0].detach().numpy()
        scores = softmax(scores)
    
        pos_total += scores[config.label2id['positive']]
        neu_total += scores[config.label2id['neutral']]
        neg_total += scores[config.label2id['negative']]

    # Výpočet průměrného skóre
    pos = pos_total / num_chunks
    neu = neu_total / num_chunks
    neg = neg_total / num_chunks
    Sentiment=pos-neg
    # Uložení výsledků do souboru
    with open(output_file, "w", encoding="utf-8") as file:
        file.write("Sentiment,pos,neu,neg\n")
        file.write(f"{Sentiment:.4f},{pos:.4f},{neu:.4f},{neg:.4f}\n")
