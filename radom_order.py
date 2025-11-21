# generate_session_orders.py
import csv
import numpy as np

def generate_latin_square_orders(num_participants=20):
    """
    Generates a balanced Latin Square ordering of 4 conditions for a number
    of participants that is a multiple of 4.
    """
    conditions = ['Baseline', 'Gestures', 'Visual', 'Verbal']
    num_conditions = len(conditions)

    if num_participants % num_conditions != 0:
        print(f"Warning: Number of participants ({num_participants}) is not a multiple of {num_conditions}. The design will not be perfectly balanced.")

    # A base Latin Square
    base_square = [
        ['Baseline', 'Gestures', 'Visual', 'Verbal'],
        ['Gestures', 'Visual', 'Verbal', 'Baseline'],
        ['Visual', 'Verbal', 'Baseline', 'Gestures'],
        ['Verbal', 'Baseline', 'Gestures', 'Visual']
    ]

    all_orders = []
    participant_id = 1
    
    # Repeat the square until we have enough participants
    while len(all_orders) < num_participants:
        for row in base_square:
            if len(all_orders) < num_participants:
                participant_str_id = f"P{participant_id:02d}"
                all_orders.append([participant_str_id] + row)
                participant_id += 1
            else:
                break
    
    return all_orders

def save_orders_to_csv(orders, filename="participant_session_orders.csv"):
    """Saves the generated orders to a CSV file."""
    header = ['Participant_ID', 'Session_1', 'Session_2', 'Session_3', 'Session_4']
    with open(filename, 'w', newline='') as f:
        writer = csv.writer(f)
        writer.writerow(header)
        writer.writerows(orders)
    print(f"Successfully generated and saved session orders to '{filename}'")

# --- Main execution ---
if __name__ == "__main__":
    NUMBER_OF_PARTICIPANTS = 20
    participant_orders = generate_latin_square_orders(NUMBER_OF_PARTICIPANTS)
    save_orders_to_csv(participant_orders)