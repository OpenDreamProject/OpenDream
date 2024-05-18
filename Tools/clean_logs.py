import sys
import re

if __name__ == "__main__":
    log_files = sys.argv[1:]
    seen = set()
    lines = []

    warning_pattern = re.compile(r'##\[warning\].*') # catch warnings might want to catch errors?

    for log_file in log_files:
        with open(log_file, 'r') as log_in:
            lines = log_in.readlines()
        with open(log_file, 'w') as log_out:
            for line in lines:
                if warning_pattern.search(line):
                    if line not in seen:
                        seen.add(line)
                        log_out.write(line)
                    continue
                log_out.write(line)
