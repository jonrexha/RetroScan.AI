from langchain_community.tools import WikipediaQueryRun, DuckDuckGoSearchRun
from langchain_community.utilities import WikipediaAPIWrapper
from langchain_core.tools import tool
from datetime import datetime

@tool("save_text_to_file", description="Saves structured research data to a text file.")
def save_to_txt(data: str, filename: str = "research_output.txt"):
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    formatted_text = f"--- Research Output ---\nTimestamp: {timestamp}\n\n{data}\n\n"

    with open(filename, "a", encoding="utf-8") as f:
        f.write(formatted_text)
    
    return f"Data successfully saved to {filename}"

save_tool = save_to_txt

search = DuckDuckGoSearchRun()
search_tool = tool(
    "search",
    search,
    description="Search the web for information",
)

api_wrapper = WikipediaAPIWrapper(top_k_results=1, doc_content_chars_max=100)
wiki_run = WikipediaQueryRun(api_wrapper=api_wrapper)
wiki_tool = tool(
    "wiki",
    wiki_run,
    description="Search Wikipedia for information",
)

