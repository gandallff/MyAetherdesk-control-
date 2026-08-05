export class SessionIDGenerator {
  private activeIDs: Set<string> = new Set();

  /**
   * Generates a unique 9-digit ID as a string, e.g. "482910375"
   */
  public generateID(): string {
    let id: string;
    do {
      // Ensure 9 digits starting with 1-9
      const firstDigit = Math.floor(Math.random() * 9) + 1;
      const remainingDigits = Math.floor(Math.random() * 100000000).toString().padStart(8, '0');
      id = `${firstDigit}${remainingDigits}`;
    } while (this.activeIDs.has(id));

    this.activeIDs.add(id);
    return id;
  }

  public releaseID(id: string): void {
    this.activeIDs.delete(id);
  }

  public formatID(id: string): string {
    if (id.length !== 9) return id;
    return `${id.slice(0, 3)} ${id.slice(3, 6)} ${id.slice(6, 9)}`;
  }
}
